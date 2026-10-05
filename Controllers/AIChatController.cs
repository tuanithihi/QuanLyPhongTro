using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Areas.Admin.Data;
using QuanLyPhongTro.Models;
using QuanLyPhongTro.Services;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace QuanLyPhongTro.Controllers
{
    /// <summary>
    /// API Controller xử lý chatbot AI tư vấn phòng trọ.
    /// Sử dụng RAG: nạp ngữ cảnh phòng đang xem + phòng Published phù hợp kèm link thật.
    /// </summary>
    [Route("api/chat")]
    [ApiController]
    public class AIChatController : ControllerBase
    {
        private readonly DataContext _db;
        private readonly IGroqService _groq;
        private readonly ILogger<AIChatController> _logger;

        public AIChatController(DataContext db, IGroqService groq, ILogger<AIChatController> logger)
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
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new ChatResponse
                {
                    Success = false,
                    Reply = "Vui lòng nhập câu hỏi."
                });
            }

            try
            {
                // Rate limiting theo IP
                string userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                if (!_groq.CheckUserRateLimit(userIp))
                {
                    return StatusCode(429, new ChatResponse
                    {
                        Success = false,
                        Reply = "Bạn đang gửi quá nhiều tin nhắn. Vui lòng chờ 1 phút rồi thử lại nhé! 😊"
                    });
                }

                // 1. Lấy thông tin phòng đang xem (nếu có)
                tblRoom? currentRoom = null;
                if (request.CurrentRoomId.HasValue && request.CurrentRoomId > 0)
                {
                    currentRoom = await _db.Rooms
                        .AsNoTracking()
                        .Include(r => r.Property).ThenInclude(p => p!.Province)
                        .Include(r => r.Property).ThenInclude(p => p!.District)
                        .Include(r => r.Property).ThenInclude(p => p!.Landlord)
                        .Include(r => r.RoomType)
                        .Include(r => r.RoomAmenities).ThenInclude(ra => ra.Amenity)
                        .FirstOrDefaultAsync(r => r.RoomId == request.CurrentRoomId.Value);
                }

                // 2. Query danh sách phòng Published, còn trống (RAG)
                var roomsQuery = _db.Rooms
                    .AsNoTracking()
                    .Include(r => r.Property).ThenInclude(p => p!.Province)
                    .Include(r => r.Property).ThenInclude(p => p!.District)
                    .Include(r => r.Property).ThenInclude(p => p!.Landlord)
                    .Include(r => r.RoomType)
                    .Include(r => r.RoomAmenities).ThenInclude(ra => ra.Amenity)
                    .Where(r => r.Status == RoomStatus.Available
                             && r.ApprovalStatus == RoomApprovalStatus.Published
                             && r.IsPublished);

                // Ưu tiên các phòng liên quan đến câu hỏi (nếu có từ khóa hoặc ngân sách)
                string msgLower = request.Message.ToLowerInvariant();
                var candidateRooms = await roomsQuery
                    .OrderBy(r => r.RoomPrice)
                    .Take(15)
                    .ToListAsync();

                // 3. Xây dựng System Prompt với ngữ cảnh phòng và quy tắc chống bịa đặt
                string systemPrompt = BuildEnhancedSystemPrompt(currentRoom, candidateRooms);

                // 4. Gọi AI
                string reply = await _groq.AskAsync(systemPrompt, request.Message);

                return Ok(new ChatResponse
                {
                    Success = true,
                    Reply = reply
                });
            }
            catch (TimeoutException tex)
            {
                _logger.LogWarning(tex, "AI Chatbot timeout");
                return Ok(new ChatResponse
                {
                    Success = false,
                    Reply = "Dạ hệ thống trợ lý AI đang phản hồi chậm do quá tải. Anh/chị có thể liên hệ trực tiếp với chủ trọ hoặc thử lại sau vài giây nhé!"
                });
            }
            catch (HttpRequestException hex)
            {
                _logger.LogWarning(hex, "Lỗi kết nối tới Groq AI");
                return Ok(new ChatResponse
                {
                    Success = false,
                    Reply = "Dạ kết nối tới trợ lý AI hiện đang gặp gián đoạn tạm thời. Anh/chị vui lòng thử lại sau giây lát hoặc liên hệ hotline để được hỗ trợ ngay ạ."
                });
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogError(ex, "Không tìm thấy file API key");
                return Ok(new ChatResponse
                {
                    Success = false,
                    Reply = "Hệ thống tư vấn AI đang bảo trì cấu hình. Vui lòng liên hệ trực tiếp chủ trọ."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xử lý chatbot AI");
                return Ok(new ChatResponse
                {
                    Success = false,
                    Reply = "Xin lỗi anh/chị, hệ thống đang gặp sự cố nhỏ khi tạo câu trả lời. Anh/chị vui lòng thử lại sau giây lát nhé!"
                });
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  BUILD SYSTEM PROMPT — Ngữ cảnh phòng + RAG + Quy tắc nghiêm ngặt
        // ══════════════════════════════════════════════════════════════════
        public static string BuildEnhancedSystemPrompt(tblRoom? currentRoom, List<tblRoom> availableRooms)
        {
            var sb = new StringBuilder();
            var vn = CultureInfo.GetCultureInfo("vi-VN");

            sb.AppendLine("Bạn là Trợ lý AI chuyên nghiệp của sàn bất động sản cho thuê 'Phòng Trọ Mới'.");
            sb.AppendLine("QUY TẮC CỐT LÕI (BẮT BUỘC TUÂN THỦ):");
            sb.AppendLine("1. Xưng hô lịch sự, chuyên nghiệp: 'dạ', 'em' và gọi khách là 'anh/chị'.");
            sb.AppendLine("2. CHỈ tư vấn và giới thiệu các phòng CÓ THẬT trong danh sách dữ liệu được cung cấp dưới đây.");
            sb.AppendLine("3. TUYỆT ĐỐI KHÔNG BỊA PHÒNG, không bịa giá, không bịa địa chỉ hoặc đường link không có trong dữ liệu.");
            sb.AppendLine("4. Khi giới thiệu bất kỳ phòng nào, BẮT BUỘC chèn đường link theo định dạng chuẩn: [Tên phòng](/phong/{Slug}-{RoomId}).");
            sb.AppendLine("5. Nếu khách hỏi phòng ở khu vực hoặc tầm giá mà danh sách không có, hãy lịch sự thông báo không có và gợi ý phòng có sẵn gần nhất.");
            sb.AppendLine("6. Hướng dẫn khách nhấn 'Đặt lịch xem' hoặc 'Liên hệ chủ trọ' nếu ưng ý phòng nào.");
            sb.AppendLine();

            if (currentRoom != null)
            {
                sb.AppendLine(">>> NGỮ CẢNH: KHÁCH ĐANG XEM TRANG PHÒNG SAU ĐÂY <<<");
                sb.AppendLine($"- Tên/Tiêu đề: {currentRoom.Title ?? currentRoom.RoomName} (ID: {currentRoom.RoomId})");
                sb.AppendLine($"- Đường dẫn xem phòng: /phong/{currentRoom.Slug}-{currentRoom.RoomId}");
                sb.AppendLine($"- Giá thuê: {currentRoom.RoomPrice.ToString("N0", vn)} đ/tháng | Đặt cọc: {currentRoom.DefaultDeposit.ToString("N0", vn)} đ");
                sb.AppendLine($"- Diện tích: {currentRoom.Area} m² | Tầng: {currentRoom.Floor} | Sức chứa: {currentRoom.Capacity} người");
                sb.AppendLine($"- Vị trí: {currentRoom.Property?.Address}, {currentRoom.Property?.District?.Name}, {currentRoom.Property?.Province?.Name}");
                sb.AppendLine($"- Chủ trọ: {currentRoom.Property?.Landlord?.FullName ?? "Chủ nhà"} (SĐT: {currentRoom.Property?.Landlord?.Phone})");
                if (currentRoom.RoomAmenities != null && currentRoom.RoomAmenities.Any())
                {
                    var ams = string.Join(", ", currentRoom.RoomAmenities.Select(a => a.Amenity?.Name).Where(n => !string.IsNullOrEmpty(n)));
                    sb.AppendLine($"- Tiện ích: {ams}");
                }
                sb.AppendLine("Nếu khách hỏi về phòng này, hãy giải đáp chi tiết dựa trên thông tin trên.");
                sb.AppendLine();
            }

            sb.AppendLine($"DANH SÁCH PHÒNG TRỐNG ĐANG CHO THUÊ TRÊN SÀN ({availableRooms.Count} phòng):");
            sb.AppendLine("══════════════════════════════════════════════════════════════════");

            if (availableRooms.Count == 0)
            {
                sb.AppendLine("Hiện chưa có phòng nào trống phù hợp.");
            }
            else
            {
                foreach (var r in availableRooms)
                {
                    string link = $"/phong/{r.Slug}-{r.RoomId}";
                    string district = r.Property?.District?.Name ?? "";
                    string province = r.Property?.Province?.Name ?? "";
                    string loc = !string.IsNullOrEmpty(district) ? $"{district}, {province}" : province;

                    sb.AppendLine($"• [{r.Title ?? r.RoomName}]({link})");
                    sb.AppendLine($"  - Mã phòng: {r.RoomCode} | ID: {r.RoomId}");
                    sb.AppendLine($"  - Link: {link}");
                    sb.AppendLine($"  - Giá thuê: {r.RoomPrice.ToString("N0", vn)} đ/tháng | Cọc: {r.DefaultDeposit.ToString("N0", vn)} đ");
                    sb.AppendLine($"  - Diện tích: {r.Area} m² | Tầng: {r.Floor} | Loại: {r.RoomType?.RoomTypeName ?? "Phòng trọ"}");
                    if (!string.IsNullOrEmpty(loc))
                        sb.AppendLine($"  - Khu vực: {loc} (Địa chỉ: {r.Property?.Address})");
                    if (r.RoomAmenities != null && r.RoomAmenities.Any())
                    {
                        var amList = string.Join(", ", r.RoomAmenities.Select(a => a.Amenity?.Name).Where(n => !string.IsNullOrEmpty(n)));
                        sb.AppendLine($"  - Tiện ích: {amList}");
                    }
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public int? CurrentRoomId { get; set; }
    }

    public class ChatResponse
    {
        public bool Success { get; set; }
        public string Reply { get; set; } = string.Empty;
    }
}
