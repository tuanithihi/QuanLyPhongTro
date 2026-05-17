using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace QuanLyPhongTro.Services
{
    /// <summary>
    /// Service gọi Groq API (OpenAI-compatible) với API key đọc từ file text bên ngoài source code.
    /// Hỗ trợ nhiều key (mỗi dòng 1 key), tự chuyển key khi bị rate-limit.
    /// 
    /// ✅ Các tính năng:
    /// - Thread-safe key rotation (SemaphoreSlim)
    /// - Exponential backoff khi bị rate-limit
    /// - Response caching cho câu hỏi trùng lặp
    /// - Đường dẫn file key cấu hình qua appsettings.json
    /// - Rate limiting phía server (giới hạn request/user/phút)
    /// </summary>
    public class GroqService
    {
        // ── Endpoint Groq (OpenAI-compatible) ────────────────────────────
        private const string GROQ_URL =
            "https://api.groq.com/openai/v1/chat/completions";

        // ── Model mặc định ───────────────────────────────────────────────
        private const string DEFAULT_MODEL = "llama-3.3-70b-versatile";

        // ── Cấu hình mặc định ────────────────────────────────────────────
        private const int KEY_CACHE_MINUTES = 5;           // Cache key file 5 phút
        private const int RESPONSE_CACHE_MINUTES = 10;     // Cache response 10 phút
        private const int MAX_BACKOFF_SECONDS = 30;        // Tối đa chờ 30 giây
        private const int USER_RATE_LIMIT_PER_MINUTE = 5;  // Mỗi user tối đa 5 request/phút

        private readonly HttpClient _httpClient;
        private readonly ILogger<GroqService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _memoryCache;

        // ── Thread-safe key management ───────────────────────────────────
        private string[] _apiKeys = Array.Empty<string>();
        private int _currentKeyIndex = 0;
        private DateTime _keysLoadedAt = DateTime.MinValue;
        private readonly SemaphoreSlim _keyLock = new(1, 1);

        // ── Rate limiting per user (IP-based) ────────────────────────────
        private readonly ConcurrentDictionary<string, UserRateInfo> _userRates = new();

        public GroqService(
            HttpClient httpClient,
            ILogger<GroqService> logger,
            IConfiguration configuration,
            IMemoryCache memoryCache)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
            _memoryCache = memoryCache;
        }

        // ══════════════════════════════════════════════════════════════════
        //  ĐỌC API KEY TỪ FILE — Thread-safe, cache 5 phút
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Lấy đường dẫn file API key từ appsettings.json (GroqApi:KeyFilePath).
        /// Fallback về đường dẫn mặc định nếu không cấu hình.
        /// </summary>
        private string GetKeyFilePath()
        {
            return _configuration["GroqApi:KeyFilePath"]
                ?? @"E:\VSCode\DemoApp\API\groq_api_key.txt";
        }

        /// <summary>
        /// Đọc API key — thread-safe với SemaphoreSlim.
        /// Cache 5 phút để tránh đọc file liên tục.
        /// </summary>
        private async Task<string> GetApiKeyAsync()
        {
            await _keyLock.WaitAsync();
            try
            {
                // Reload nếu cache hết hạn hoặc chưa load
                if (_apiKeys.Length == 0 || (DateTime.Now - _keysLoadedAt).TotalMinutes > KEY_CACHE_MINUTES)
                {
                    LoadKeysFromFile();
                }

                if (_apiKeys.Length == 0)
                    throw new InvalidOperationException(
                        $"Không tìm thấy API key hợp lệ trong file: {GetKeyFilePath()}");

                return _apiKeys[_currentKeyIndex % _apiKeys.Length];
            }
            finally
            {
                _keyLock.Release();
            }
        }

        /// <summary>
        /// Chuyển sang API key tiếp theo — thread-safe.
        /// Xoay vòng circular: 0 → 1 → 2 → ... → 0
        /// </summary>
        private async Task RotateToNextKeyAsync()
        {
            await _keyLock.WaitAsync();
            try
            {
                if (_apiKeys.Length <= 1) return;
                _currentKeyIndex = (_currentKeyIndex + 1) % _apiKeys.Length;
                _logger.LogWarning(
                    "⚠ Groq API key bị limit, chuyển sang key #{Index}/{Total}",
                    _currentKeyIndex + 1, _apiKeys.Length);
            }
            finally
            {
                _keyLock.Release();
            }
        }

        /// <summary>
        /// Đọc tất cả key từ file text.
        /// Trim khoảng trắng, bỏ dòng trống, bỏ dòng comment (#).
        /// </summary>
        private void LoadKeysFromFile()
        {
            string keyFilePath = GetKeyFilePath();

            if (!File.Exists(keyFilePath))
                throw new FileNotFoundException(
                    $"File API key không tồn tại: {keyFilePath}. " +
                    "Vui lòng tạo file này với mỗi dòng chứa 1 API key.");

            var lines = File.ReadAllLines(keyFilePath)
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith("#"))  // Bỏ comment
                .ToArray();

            if (lines.Length == 0)
                throw new InvalidOperationException(
                    $"File {keyFilePath} không chứa API key hợp lệ nào.");

            _apiKeys = lines;
            _keysLoadedAt = DateTime.Now;
            _logger.LogInformation("✅ Đã load {Count} Groq API key(s) từ file.", lines.Length);
        }

        // ══════════════════════════════════════════════════════════════════
        //  RATE LIMITING — Giới hạn request/user/phút
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Kiểm tra user (theo IP hoặc ID) có vượt giới hạn request/phút không.
        /// Trả về true nếu được phép, false nếu bị giới hạn.
        /// </summary>
        public bool CheckUserRateLimit(string userId)
        {
            var now = DateTime.Now;
            var rateInfo = _userRates.GetOrAdd(userId, _ => new UserRateInfo());

            lock (rateInfo)
            {
                // Reset counter nếu đã qua 1 phút
                if ((now - rateInfo.WindowStart).TotalMinutes >= 1)
                {
                    rateInfo.WindowStart = now;
                    rateInfo.RequestCount = 0;
                }

                rateInfo.RequestCount++;

                if (rateInfo.RequestCount > USER_RATE_LIMIT_PER_MINUTE)
                {
                    _logger.LogWarning(
                        "🚫 User {UserId} vượt giới hạn {Limit} request/phút (đã gửi {Count})",
                        userId, USER_RATE_LIMIT_PER_MINUTE, rateInfo.RequestCount);
                    return false;
                }

                return true;
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  GỌI GROQ API — Retry + Rotate + Backoff + Cache
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Gọi Groq API với system instruction + câu hỏi người dùng.
        /// 
        /// Chiến lược xử lý rate-limit:
        /// 1. Retry với key khác khi bị HTTP 429
        /// 2. Exponential backoff (1s → 2s → 4s → 8s → ... max 30s)
        /// 3. Cache response cho câu hỏi trùng lặp
        /// 4. Thread-safe key rotation
        /// </summary>
        public async Task<string> AskAsync(string systemPrompt, string userMessage)
        {
            // ── Kiểm tra cache trước — tránh gọi API lặp ────────────────
            string cacheKey = $"groq:{ComputeHash(systemPrompt + "|" + userMessage)}";
            if (_memoryCache.TryGetValue(cacheKey, out string? cachedResponse) && cachedResponse != null)
            {
                _logger.LogInformation("📦 Cache hit — trả response từ cache");
                return cachedResponse;
            }

            // ── Đảm bảo load key trước ───────────────────────────────────
            await GetApiKeyAsync(); // force load
            int maxRetries = Math.Max(_apiKeys.Length, 1);

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    string apiKey = await GetApiKeyAsync();

                    // ── Tạo request body theo format OpenAI-compatible ────
                    var requestBody = new
                    {
                        model = DEFAULT_MODEL,
                        messages = new[]
                        {
                            new { role = "system", content = systemPrompt },
                            new { role = "user", content = userMessage }
                        },
                        temperature = 0.7,
                        max_completion_tokens = 1024,
                        top_p = 0.9
                    };

                    var json = JsonSerializer.Serialize(requestBody);
                    var httpRequest = new HttpRequestMessage(HttpMethod.Post, GROQ_URL)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };

                    // ── Auth: Bearer token ───────────────────────────────
                    httpRequest.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", apiKey);

                    // ── Gửi request ─────────────────────────────────────
                    var response = await _httpClient.SendAsync(httpRequest);

                    // ── Xử lý rate-limit: rotate key + exponential backoff ─
                    if ((int)response.StatusCode == 429)
                    {
                        _logger.LogWarning(
                            "⚠ Groq API trả về 429 (rate-limit), attempt {Attempt}/{Max}",
                            attempt + 1, maxRetries);

                        await RotateToNextKeyAsync();

                        // Exponential backoff: 1s, 2s, 4s, 8s... (max 30s)
                        int delaySeconds = Math.Min(
                            (int)Math.Pow(2, attempt),
                            MAX_BACKOFF_SECONDS);

                        _logger.LogInformation(
                            "⏳ Chờ {Delay}s trước khi retry...", delaySeconds);
                        await Task.Delay(TimeSpan.FromSeconds(delaySeconds));

                        continue; // Retry với key mới
                    }

                    response.EnsureSuccessStatusCode();

                    // ── Parse response JSON ─────────────────────────────
                    var responseJson = await response.Content.ReadAsStringAsync();
                    string result = ParseGroqResponse(responseJson);

                    // ── Cache response thành công ────────────────────────
                    _memoryCache.Set(cacheKey, result, TimeSpan.FromMinutes(RESPONSE_CACHE_MINUTES));
                    _logger.LogInformation("💾 Đã cache response (TTL: {Minutes} phút)", RESPONSE_CACHE_MINUTES);

                    return result;
                }
                catch (HttpRequestException ex) when (attempt < maxRetries - 1)
                {
                    _logger.LogWarning(ex,
                        "⚠ Groq API call thất bại, attempt {Attempt}/{Max}",
                        attempt + 1, maxRetries);

                    await RotateToNextKeyAsync();

                    // Backoff cả khi gặp lỗi HTTP khác
                    int delaySeconds = Math.Min(
                        (int)Math.Pow(2, attempt),
                        MAX_BACKOFF_SECONDS);
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                }
            }

            throw new Exception(
                "Tất cả API key đều không phản hồi. Vui lòng kiểm tra lại. " +
                $"Đã thử {maxRetries} key với exponential backoff.");
        }

        // ══════════════════════════════════════════════════════════════════
        //  PARSE RESPONSE — Trích xuất text từ JSON trả về của Groq
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Parse response theo format OpenAI-compatible:
        /// { choices: [{ message: { role: "assistant", content: "..." } }] }
        /// </summary>
        private string ParseGroqResponse(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // Cấu trúc: { choices: [{ message: { content: "..." } }] }
                if (root.TryGetProperty("choices", out var choices) &&
                    choices.GetArrayLength() > 0)
                {
                    var firstChoice = choices[0];
                    if (firstChoice.TryGetProperty("message", out var message) &&
                        message.TryGetProperty("content", out var content))
                    {
                        return content.GetString() ?? "Xin lỗi, tôi không thể trả lời lúc này.";
                    }
                }

                // Kiểm tra lỗi từ API
                if (root.TryGetProperty("error", out var error))
                {
                    var errorMessage = error.GetProperty("message").GetString();
                    _logger.LogError("Groq API error: {Message}", errorMessage);
                    return $"Hệ thống gặp lỗi: {errorMessage}";
                }

                return "Xin lỗi, tôi không thể trả lời câu hỏi này lúc này.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi parse Groq response: {Json}", json[..Math.Min(500, json.Length)]);
                return "Xin lỗi, đã có lỗi xảy ra khi xử lý phản hồi từ AI.";
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  HELPER — Tạo hash cho cache key
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Tạo hash SHA256 ngắn gọn để làm cache key.
        /// </summary>
        private static string ComputeHash(string input)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes)[..16]; // Lấy 16 ký tự đầu
        }

        // ══════════════════════════════════════════════════════════════════
        //  INNER CLASS — Thông tin rate limit per user
        // ══════════════════════════════════════════════════════════════════

        private class UserRateInfo
        {
            public DateTime WindowStart { get; set; } = DateTime.Now;
            public int RequestCount { get; set; } = 0;
        }
    }
}
