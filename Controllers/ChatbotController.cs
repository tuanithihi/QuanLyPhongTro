using Microsoft.AspNetCore.Mvc;
using QuanLyThuVien.Models;
using QuanLyThuVien.Services;
using QuanLyThuVien.Utilities;
using System.Text.Json;

namespace QuanLyThuVien.Controllers
{
    [ApiController]
    [Route("api/chatbot")]
    public class ChatbotController : ControllerBase
    {
        private readonly ChatbotService _chatbotService;
        private readonly IConfiguration _config;

        public ChatbotController(ChatbotService chatbotService, IConfiguration config)
        {
            _chatbotService = chatbotService;
            _config = config;
        }

        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] ChatRequest req)
        {
            if (string.IsNullOrEmpty(req.Message))
                return Ok(new { reply = "Vui lòng nhập câu hỏi." });

            var apiKey = _config["OpenRouter:ApiKey"];

            if (string.IsNullOrEmpty(apiKey))
                return Ok(new { reply = "API Key chưa được cấu hình." });

            try
            {
                // Lấy UserID nếu đã đăng nhập
                int? currentUserId = Functions.IsLogin() ? Functions._UserID : null;
                string? currentUserName = Functions.IsLogin() ? Functions._FullName : null;
                
                // SỬ DỤNG ChatbotService để đọc FULL dữ liệu từ database (có cả dữ liệu user nếu đã đăng nhập)
                var reply = await _chatbotService.Ask(req.Message, currentUserId, currentUserName);
                return Ok(new { reply });
            }
            catch (Exception ex)
            {
                return Ok(new { reply = $"Lỗi hệ thống: {ex.Message}" });
            }
        }
    }
}
