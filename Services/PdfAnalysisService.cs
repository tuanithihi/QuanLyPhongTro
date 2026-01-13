using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using Microsoft.Extensions.Caching.Memory;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace QuanLyThuVien.Services
{
    public class PdfAnalysisService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly ILogger<PdfAnalysisService> _logger;
        private readonly string? _ocrApiKey;
        private readonly string? _geminiApiKey;

        public PdfAnalysisService(
            HttpClient httpClient,
            IConfiguration configuration,
            IMemoryCache cache,
            ILogger<PdfAnalysisService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _cache = cache;
            _logger = logger;
            _ocrApiKey = _configuration["OcrSpaceApiKey"];
            _geminiApiKey = _configuration["OpenRouter:ApiKey"];

            _httpClient.Timeout = TimeSpan.FromSeconds(120);
        }

        /// <summary>
        /// Extract text from PDF file
        /// </summary>
        public async Task<string> ExtractTextFromPdf(string pdfPath, int maxPages = 50)
        {
            try
            {
                // Chuẩn hóa đường dẫn - chuyển URL request thành đường dẫn vật lý
                var physicalPath = ConvertToPhysicalPath(pdfPath);

                _logger.LogInformation("PDF Path conversion: {Original} -> {Physical}", pdfPath, physicalPath);

                if (!File.Exists(physicalPath))
                {
                    _logger.LogWarning("PDF file not found at: {Path}", physicalPath);
                    return $"Không tìm thấy file PDF tại: {physicalPath}";
                }

                // Try OCR.space API first for better accuracy
                if (!string.IsNullOrEmpty(_ocrApiKey))
                {
                    try
                    {
                        var ocrText = await ExtractTextUsingOCR(physicalPath);
                        if (!string.IsNullOrWhiteSpace(ocrText) && ocrText.Length > 100)
                        {
                            _logger.LogInformation("Successfully extracted text using OCR API");
                            return ocrText;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "OCR API failed, falling back to PdfPig");
                    }
                }

                // Fallback to PdfPig for text-based PDFs
                var textBuilder = new StringBuilder();
                using (var pdf = PdfDocument.Open(physicalPath))
                {
                    var pagesToRead = maxPages == 0 ? pdf.NumberOfPages : Math.Min(pdf.NumberOfPages, maxPages);
                    
                    for (int i = 1; i <= pagesToRead; i++)
                    {
                        var page = pdf.GetPage(i);
                        var pageText = page.Text;
                        
                        if (!string.IsNullOrWhiteSpace(pageText))
                        {
                            textBuilder.AppendLine($"--- Trang {i} ---");
                            textBuilder.AppendLine(pageText);
                            textBuilder.AppendLine();
                        }
                    }

                    if (maxPages > 0 && pdf.NumberOfPages > maxPages)
                    {
                        textBuilder.AppendLine($"\n[Chỉ đọc {maxPages} trang đầu tiên. Tổng số trang: {pdf.NumberOfPages}]");
                    }
                }

                var extractedText = textBuilder.ToString();
                
                // Giới hạn độ dài text để tránh quá tải
                if (extractedText.Length > 50000)
                {
                    extractedText = extractedText.Substring(0, 50000) + "\n[Văn bản quá dài, đã cắt bớt...]";
                }

                return string.IsNullOrWhiteSpace(extractedText) 
                    ? "Không thể trích xuất văn bản từ PDF (có thể là PDF hình ảnh hoặc file bị mã hóa)" 
                    : extractedText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi extract text từ PDF: {Path}", pdfPath);
                return $"Lỗi khi đọc file: {ex.Message}";
            }
        }

        /// <summary>
        /// Extract text using OCR.space API (better for image-based PDFs)
        /// </summary>
        private async Task<string> ExtractTextUsingOCR(string pdfPath)
        {
            try
            {
                using var form = new MultipartFormDataContent();
                
                // Read PDF file
                var fileBytes = await File.ReadAllBytesAsync(pdfPath);
                _logger.LogInformation("OCR: Processing PDF {FileName}, Size: {Size} bytes", Path.GetFileName(pdfPath), fileBytes.Length);
                
                var fileContent = new ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                form.Add(fileContent, "file", Path.GetFileName(pdfPath));
                
                // OCR.space API parameters
                form.Add(new StringContent(_ocrApiKey ?? ""), "apikey");
                form.Add(new StringContent("true"), "isOverlayRequired");
                form.Add(new StringContent("2"), "OCREngine"); // Engine 2: Better quality
                form.Add(new StringContent("true"), "detectOrientation");
                form.Add(new StringContent("true"), "scale");
                form.Add(new StringContent("true"), "isTable");
                
                var response = await _httpClient.PostAsync("https://api.ocr.space/parse/image", form);
                var jsonResponse = await response.Content.ReadAsStringAsync();
                
                _logger.LogInformation("OCR Response: {Response}", jsonResponse.Length > 500 ? jsonResponse.Substring(0, 500) + "..." : jsonResponse);
                
                response.EnsureSuccessStatusCode();
                
                // Use JsonDocument to parse flexibly
                using var doc = System.Text.Json.JsonDocument.Parse(jsonResponse);
                var root = doc.RootElement;
                
                // Check for errors
                if (root.TryGetProperty("OCRExitCode", out var exitCodeElement) && exitCodeElement.GetInt32() > 1)
                {
                    var errorMsg = root.TryGetProperty("ErrorMessage", out var errElement) 
                        ? (errElement.ValueKind == System.Text.Json.JsonValueKind.Array 
                            ? string.Join(", ", errElement.EnumerateArray().Select(e => e.GetString()))
                            : errElement.GetString())
                        : "Unknown error";
                    
                    _logger.LogWarning("OCR API returned error code {Code}: {Message}", exitCodeElement.GetInt32(), errorMsg);
                    return string.Empty;
                }
                
                if (root.TryGetProperty("IsErroredOnProcessing", out var isErrorElement) && isErrorElement.GetBoolean())
                {
                    var errorMsg = root.TryGetProperty("ErrorMessage", out var errElement)
                        ? (errElement.ValueKind == System.Text.Json.JsonValueKind.Array
                            ? string.Join(", ", errElement.EnumerateArray().Select(e => e.GetString()))
                            : errElement.GetString())
                        : "Unknown processing error";
                    
                    _logger.LogWarning("OCR processing error: {Message}", errorMsg);
                    return string.Empty;
                }
                
                // Parse results
                if (root.TryGetProperty("ParsedResults", out var resultsElement) && resultsElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    var textBuilder = new StringBuilder();
                    int pageNum = 1;
                    
                    foreach (var result in resultsElement.EnumerateArray())
                    {
                        if (result.TryGetProperty("ParsedText", out var textElement))
                        {
                            var text = textElement.GetString();
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                textBuilder.AppendLine($"--- Trang {pageNum} ---");
                                textBuilder.AppendLine(text);
                                textBuilder.AppendLine();
                                pageNum++;
                            }
                        }
                    }
                    
                    var extractedText = textBuilder.ToString();
                    _logger.LogInformation("OCR extracted {Length} characters from {Pages} pages", extractedText.Length, pageNum - 1);
                    return extractedText;
                }
                
                _logger.LogWarning("OCR returned no results");
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OCR.space API failed");
                throw;
            }
        }

        // OCR.space API response models
        private class OcrSpaceResponse
        {
            [System.Text.Json.Serialization.JsonPropertyName("ParsedResults")]
            public List<ParsedResult>? ParsedResults { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("OCRExitCode")]
            public int? OCRExitCode { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("ErrorMessage")]
            public object? ErrorMessage { get; set; } // Can be string or array
            
            [System.Text.Json.Serialization.JsonPropertyName("IsErroredOnProcessing")]
            public bool? IsErroredOnProcessing { get; set; }
            
            public string GetErrorMessage()
            {
                if (ErrorMessage == null) return string.Empty;
                
                if (ErrorMessage is System.Text.Json.JsonElement element)
                {
                    if (element.ValueKind == System.Text.Json.JsonValueKind.String)
                        return element.GetString() ?? string.Empty;
                    
                    if (element.ValueKind == System.Text.Json.JsonValueKind.Array)
                        return string.Join(", ", element.EnumerateArray().Select(e => e.GetString()));
                }
                
                return ErrorMessage.ToString() ?? string.Empty;
            }
        }

        private class ParsedResult
        {
            [System.Text.Json.Serialization.JsonPropertyName("ParsedText")]
            public string? ParsedText { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("ErrorCode")]
            public int? ErrorCode { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("ErrorMessage")]
            public string? ErrorMessage { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("FileParseExitCode")]
            public int? FileParseExitCode { get; set; }
        }

        /// <summary>
        /// Chuyển đổi đường dẫn web/URL thành đường dẫn vật lý
        /// </summary>
        private string ConvertToPhysicalPath(string webPath)
        {
            var currentDir = Directory.GetCurrentDirectory();
            
            // URL decode để xử lý %20, %2F, etc.
            var decodedPath = Uri.UnescapeDataString(webPath);
            _logger.LogInformation("Path decode: {Original} → {Decoded}", webPath, decodedPath);
            
            // Nếu đã là đường dẫn tuyệt đối và tồn tại, dùng luôn
            if (Path.IsPathRooted(decodedPath) && File.Exists(decodedPath))
            {
                return decodedPath;
            }

            // Xử lý URL web path - chuẩn hóa
            var path = decodedPath.TrimStart('~').TrimStart('/').Replace('/', '\\');
            var fileName = Path.GetFileName(path);

            // Danh sách các đường dẫn có thể
            var possiblePaths = new List<string>();

            // 1. Trực tiếp trong uploads/ (root level)
            possiblePaths.Add(Path.Combine(currentDir, "uploads", fileName));

            // 2. Trong uploads/books/pdfs/ (thư mục chuẩn cho PDFs)
            possiblePaths.Add(Path.Combine(currentDir, "uploads", "books", "pdfs", fileName));

            // 3. Chuyển /files/ thành uploads/ (theo cấu hình trong Program.cs)
            if (path.StartsWith("files\\", StringComparison.OrdinalIgnoreCase))
            {
                var relativePath = path.Substring(6); // Bỏ "files\"
                possiblePaths.Add(Path.Combine(currentDir, "uploads", relativePath));
                possiblePaths.Add(Path.Combine(currentDir, "uploads", "books", "pdfs", Path.GetFileName(relativePath)));
            }

            // 4. Nếu là uploads/ trực tiếp trong path
            if (path.StartsWith("uploads\\", StringComparison.OrdinalIgnoreCase))
            {
                possiblePaths.Add(Path.Combine(currentDir, path));
            }
            
            // 5. Thử trong wwwroot/uploads/ (cho static files)
            possiblePaths.Add(Path.Combine(currentDir, "wwwroot", "uploads", fileName));
            possiblePaths.Add(Path.Combine(currentDir, "wwwroot", "uploads", "books", "pdfs", fileName));

            // 6. Đường dẫn đầy đủ từ path gốc
            if (path.Contains("\\"))
            {
                possiblePaths.Add(Path.Combine(currentDir, "uploads", path));
            }

            // Loại bỏ duplicates
            possiblePaths = possiblePaths.Distinct().ToList();

            // Tìm file đầu tiên tồn tại
            foreach (var possiblePath in possiblePaths)
            {
                _logger.LogInformation("Checking path: {Path} - Exists: {Exists}", possiblePath, File.Exists(possiblePath));
                if (File.Exists(possiblePath))
                {
                    _logger.LogInformation("✓ PDF found at: {Path}", possiblePath);
                    return possiblePath;
                }
            }

            // Nếu không tìm thấy, log tất cả các đường dẫn đã thử
            _logger.LogError("❌ PDF NOT FOUND after trying {Count} paths. Original: {Original}, Decoded: {Decoded}", 
                possiblePaths.Count, webPath, decodedPath);
            foreach (var p in possiblePaths)
            {
                _logger.LogError("  → Tried: {Path}", p);
            }
            
            // Trả về đường dẫn đầu tiên (có thể dùng để hiển thị lỗi)
            return possiblePaths.First();
        }

        /// <summary>
        /// Analyze PDF content using Gemini AI
        /// </summary>
        public async Task<string> AnalyzePdfContent(string pdfPath, string bookTitle, string analysisType = "summary")
        {
            try
            {
                // Check cache first
                var cacheKey = $"pdf_analysis_{pdfPath}_{analysisType}";
                if (_cache.TryGetValue(cacheKey, out string? cachedResult) && cachedResult != null)
                {
                    return cachedResult;
                }

                // Extract text from PDF
                var extractedText = await ExtractTextFromPdf(pdfPath, maxPages: 30);
                
                if (extractedText.StartsWith("Lỗi") || extractedText.StartsWith("Không"))
                {
                    return extractedText;
                }

                // Build prompt based on analysis type
                var prompt = BuildAnalysisPrompt(bookTitle, extractedText, analysisType);

                // Call Gemini API
                var result = await CallGeminiApi(prompt);

                // Cache result for 30 minutes
                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(30));

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi phân tích PDF: {Path}", pdfPath);
                return $"Lỗi phân tích: {ex.Message}";
            }
        }

        /// <summary>
        /// Build analysis prompt for different types
        /// </summary>
        private string BuildAnalysisPrompt(string bookTitle, string content, string analysisType)
        {
            var basePrompt = $"Bạn là chuyên gia phân tích sách. Đây là nội dung từ sách '{bookTitle}':\n\n{content}\n\n";

            return analysisType.ToLower() switch
            {
                "summary" => basePrompt + @"Hãy TÓM TẮT nội dung sách một cách chi tiết, bao gồm:
1. Ý chính của sách
2. Các chủ đề chính được đề cập
3. Thông tin quan trọng
4. Kết luận chính

Trả lời bằng tiếng Việt, rõ ràng và dễ hiểu.",

                "keywords" => basePrompt + @"Hãy TRÍCH XUẤT các từ khóa và khái niệm quan trọng nhất từ sách này.
Chia thành các nhóm:
1. Từ khóa chính (5-10 từ)
2. Khái niệm quan trọng (5-10 khái niệm)
3. Các chủ đề liên quan

Trả lời bằng tiếng Việt.",

                "structure" => basePrompt + @"Hãy PHÂN TÍCH cấu trúc và nội dung của sách:
1. Cách tổ chức nội dung
2. Các phần/chương chính (nếu nhận ra)
3. Mục đích của từng phần
4. Trình tự logic của nội dung

Trả lời bằng tiếng Việt.",

                "questions" => basePrompt + @"Dựa trên nội dung sách, hãy:
1. Đưa ra 5-10 câu hỏi quan trọng mà sách này trả lời
2. Đưa ra 3-5 câu hỏi thảo luận sâu hơn cho người đọc
3. Gợi ý hướng nghiên cứu thêm

Trả lời bằng tiếng Việt.",

                "target_audience" => basePrompt + @"Hãy ĐÁNH GIÁ:
1. Sách này phù hợp với đối tượng độc giả nào?
2. Yêu cầu kiến thức nền tảng (nếu có)
3. Mục đích sử dụng (học tập, nghiên cứu, tham khảo, giải trí...)
4. Độ khó của nội dung (cơ bản/trung bình/nâng cao)

Trả lời bằng tiếng Việt.",

                _ => basePrompt + "Hãy phân tích và tóm tắt nội dung sách này bằng tiếng Việt."
            };
        }

        /// <summary>
        /// Call Gemini API
        /// </summary>
        private async Task<string> CallGeminiApi(string prompt)
        {
            try
            {
                if (string.IsNullOrEmpty(_geminiApiKey))
                {
                    return "Chưa cấu hình Gemini API Key";
                }

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        maxOutputTokens = 2048
                    }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_geminiApiKey}";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Gemini API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                    return $"Lỗi API: {response.StatusCode}";
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonDocument.Parse(responseContent);

                var textResult = jsonResponse.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return textResult ?? "Không có kết quả";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gọi Gemini API");
                return $"Lỗi: {ex.Message}";
            }
        }

        /// <summary>
        /// Get quick summary of PDF (first few pages only)
        /// </summary>
        public async Task<string> GetQuickSummary(string pdfPath, string bookTitle)
        {
            var extractedText = await ExtractTextFromPdf(pdfPath, maxPages: 5);
            
            if (extractedText.StartsWith("Lỗi") || extractedText.StartsWith("Không"))
            {
                return extractedText;
            }

            var prompt = $"Đây là 5 trang đầu của sách '{bookTitle}':\n\n{extractedText}\n\nHãy tóm tắt ngắn gọn (3-5 câu) nội dung chính và mục đích của sách. Trả lời bằng tiếng Việt.";
            
            return await CallGeminiApi(prompt);
        }
    }
}
