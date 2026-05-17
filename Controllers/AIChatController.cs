using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Areas.Admin.Data;
using QuanLyPhongTro.Models;
using QuanLyPhongTro.Services;
using System.Globalization;
using System.Text;

namespace QuanLyPhongTro.Controllers
{
    /// <summary>
    /// API Controller xử lý chatbot AI tư vấn phòng trọ.
    /// Sử dụng RAG: truy vấn DB lấy dữ liệu phòng thực → ghép vào prompt → gọi Groq.
    /// </summary>
    [Route("api/chat")]
    [ApiController]
    public class AIChatController : ControllerBase
    {
        private readonly DataContext _db;
        private readonly GroqService _groq;
        private readonly ILogger<AIChatController> _logger;

        public AIChatController(DataContext db, GroqService groq, ILogger<AIChatController> logger)
        {
            _db = db;
            _groq = groq;
            _logger = logger;
        }

        // ══════════════════════════════════════════════════════════════════
        //  POST /api/chat/ask — Nhận câu hỏi, trả lời tư vấn phòng
        // ══════════════════════════════════════════════════════════════════

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] ChatRequest request)
        {
            // ── Validate đầu vào ─────────────────────────────────────────
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
                return BadRequest(new ChatResponse
                {
                    Success = false,
                    Reply = "Vui lòng nhập câu hỏi."
                });

            try
            {
                // ── Rate limiting — giới hạn request/user/phút ───────────
                string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                if (!_groq.CheckUserRateLimit(userIp))
                {
                    return StatusCode(429, new ChatResponse
                    {
                        Success = false,
                        Reply = "Bạn đang gửi quá nhiều tin nhắn. Vui lòng chờ 1 phút rồi thử lại nhé! 😊"
                    });
                }

                // ── Bước 1: Query database — lấy phòng trống (RAG) ──────
                var availableRooms = await _db.Rooms
                    .Include(r => r.RoomType)
                    .Where(r => r.Status == RoomStatus.Available && r.IsPublished)
                    .OrderBy(r => r.RoomPrice)
                    .ToListAsync();

                // ── Bước 2: Tạo System Prompt + dữ liệu phòng ──────────
                string systemPrompt = BuildSystemPrompt(availableRooms);

                // ── Bước 3: Gọi Groq API ───────────────────────────────
                string reply = await _groq.AskAsync(systemPrompt, request.Message);

                return Ok(new ChatResponse
                {
                    Success = true,
                    Reply = reply
                });
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogError(ex, "Không tìm thấy file API key");
                return StatusCode(500, new ChatResponse
                {
                    Success = false,
                    Reply = "Hệ thống chưa được cấu hình API key. Vui lòng liên hệ quản trị viên."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xử lý chatbot AI");
                return StatusCode(500, new ChatResponse
                {
                    Success = false,
                    Reply = "Xin lỗi, hệ thống đang gặp sự cố. Vui lòng thử lại sau."
                });
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  XÂY DỰNG SYSTEM PROMPT — Vai trò + dữ liệu phòng thực
        // ══════════════════════════════════════════════════════════════════

        private static string BuildSystemPrompt(List<tblRoom> rooms)
        {
            var sb = new StringBuilder();
            var vn = CultureInfo.GetCultureInfo("vi-VN");

            // ── Thiết lập vai trò cho AI ─────────────────────────────────
            sb.AppendLine("Bạn là nhân viên tư vấn phòng trọ của hệ thống 'Phòng Trọ Mới'.");
            sb.AppendLine("Quy tắc giao tiếp:");
            sb.AppendLine("- Xưng hô lịch sự: 'dạ', 'vâng', 'em' (gọi khách là 'anh/chị').");
            sb.AppendLine("- Nhiệt tình, thân thiện, chuyên nghiệp.");
            sb.AppendLine("- Chỉ tư vấn dựa trên dữ liệu phòng thực được cung cấp bên dưới.");
            sb.AppendLine("- TUYỆT ĐỐI KHÔNG bịa thông tin phòng không có trong danh sách.");
            sb.AppendLine("- Nếu không có phòng phù hợp, nói rõ ràng và gợi ý lựa chọn gần nhất.");
            sb.AppendLine("- Luôn hỏi khách có muốn đặt lịch xem phòng không sau khi tư vấn.");
            sb.AppendLine("- Trả lời ngắn gọn, rõ ràng, có gạch đầu dòng khi liệt kê.");
            sb.AppendLine("- Khi khách hỏi về giá, luôn kèm theo thông tin diện tích, tầng, tiện ích.");
            sb.AppendLine();

            // ── Ghép dữ liệu phòng trống từ database ────────────────────
            if (rooms.Count == 0)
            {
                sb.AppendLine("THÔNG BÁO: Hiện tại KHÔNG CÓ phòng trống nào.");
                sb.AppendLine("Hãy thông báo cho khách và đề nghị để lại thông tin liên hệ.");
            }
            else
            {
                sb.AppendLine($"DANH SÁCH PHÒNG TRỐNG ({rooms.Count} phòng):");
                sb.AppendLine("═══════════════════════════════════════");

                foreach (var room in rooms)
                {
                    sb.AppendLine($"• Phòng: {room.RoomName} (Mã: {room.RoomCode})");
                    sb.AppendLine($"  - Loại: {room.RoomType?.RoomTypeName ?? "Chưa phân loại"}");
                    sb.AppendLine($"  - Giá thuê: {room.RoomPrice.ToString("N0", vn)} VNĐ/tháng");
                    sb.AppendLine($"  - Đặt cọc: {room.DefaultDeposit.ToString("N0", vn)} VNĐ");

                    if (room.Area > 0)
                        sb.AppendLine($"  - Diện tích: {room.Area} m²");
                    if (room.Floor > 0)
                        sb.AppendLine($"  - Tầng: {room.Floor}");
                    if (room.MaxOccupants > 0)
                        sb.AppendLine($"  - Số người tối đa: {room.MaxOccupants}");
                    if (!string.IsNullOrWhiteSpace(room.Address))
                        sb.AppendLine($"  - Địa chỉ: {room.Address}");
                    if (!string.IsNullOrWhiteSpace(room.Description))
                        sb.AppendLine($"  - Mô tả: {room.Description}");

                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  DTO — Request / Response models
    // ══════════════════════════════════════════════════════════════════════

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
    }

    public class ChatResponse
    {
        public bool Success { get; set; }
        public string Reply { get; set; } = string.Empty;
    }
}
