using Google.Cloud.TextToSpeech.V1;
using System.Text.RegularExpressions;

namespace QuanLyThuVien.Services
{
    public class TextToSpeechService
    {
        private readonly ILogger<TextToSpeechService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string? _apiKey;

        public TextToSpeechService(
            ILogger<TextToSpeechService> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _apiKey = _configuration["GeminiApiKey"]; // Dùng chung API key
        }

        /// <summary>
        /// Phát hiện ngôn ngữ từ văn bản
        /// </summary>
        public string DetectLanguage(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "en-US";

            var sampleText = text.Length > 1000 ? text.Substring(0, 1000) : text;

            // Đếm ký tự tiếng Việt
            var vietnamesePattern = new Regex(@"[àáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđ]", RegexOptions.IgnoreCase);
            var vietnameseCount = vietnamesePattern.Matches(sampleText).Count;

            // Nếu > 3% có dấu tiếng Việt
            if (vietnameseCount > sampleText.Length * 0.03)
                return "vi-VN";

            // Kiểm tra từ khóa tiếng Việt
            var vietnameseWords = new[] { "của", "và", "là", "có", "trong", "được", "với", "không", "này", "cho", "một", "các", "đã", "người", "năm" };
            var lowerText = sampleText.ToLower();
            var foundWords = vietnameseWords.Count(word => lowerText.Contains($" {word} "));

            if (foundWords >= 3)
                return "vi-VN";

            return "en-US";
        }

        /// <summary>
        /// Chuyển text thành audio bằng Google Cloud TTS
        /// </summary>
        public async Task<byte[]> SynthesizeSpeechAsync(string text, string? languageCode = null)
        {
            try
            {
                // Tự động phát hiện ngôn ngữ nếu không truyền vào
                var detectedLanguage = languageCode ?? DetectLanguage(text);
                
                _logger.LogInformation("Synthesizing speech for language: {Language}, text length: {Length}", 
                    detectedLanguage, text.Length);

                // Tạo client (cần GOOGLE_APPLICATION_CREDENTIALS environment variable)
                var client = await TextToSpeechClient.CreateAsync();

                // Cấu hình giọng nói
                var input = new SynthesisInput { Text = text };

                var voiceSelection = new VoiceSelectionParams
                {
                    LanguageCode = detectedLanguage,
                    SsmlGender = SsmlVoiceGender.Neutral
                };

                // Chọn giọng cụ thể theo ngôn ngữ
                if (detectedLanguage == "vi-VN")
                {
                    voiceSelection.Name = "vi-VN-Wavenet-A"; // Giọng nữ tự nhiên
                }
                else
                {
                    voiceSelection.Name = "en-US-Neural2-C"; // Giọng nữ tự nhiên
                }

                var audioConfig = new AudioConfig
                {
                    AudioEncoding = AudioEncoding.Mp3,
                    SpeakingRate = 1.0,
                    Pitch = 0.0
                };

                var response = await client.SynthesizeSpeechAsync(input, voiceSelection, audioConfig);

                _logger.LogInformation("Speech synthesis completed. Audio size: {Size} bytes", 
                    response.AudioContent.Length);

                return response.AudioContent.ToByteArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error synthesizing speech");
                throw;
            }
        }

        /// <summary>
        /// Chuyển text thành audio với fallback về Web Speech API nếu Google TTS không khả dụng
        /// </summary>
        public async Task<(byte[]? audioData, bool useWebSpeech, string language)> SynthesizeSpeechWithFallbackAsync(string text)
        {
            var language = DetectLanguage(text);

            try
            {
                var audioData = await SynthesizeSpeechAsync(text, language);
                return (audioData, false, language);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Google TTS not available, falling back to Web Speech API");
                // Trả về null để client dùng Web Speech API
                return (null, true, language);
            }
        }
    }
}
