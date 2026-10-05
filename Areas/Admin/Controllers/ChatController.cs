using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Areas.Admin.Attributes;
using QuanLyPhongTro.Areas.Admin.Data;
using QuanLyPhongTro.Models;
using QuanLyPhongTro.Services;

namespace QuanLyPhongTro.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminOnly]
    public class ChatController : Controller
    {
        private readonly DataContext _db;
        private readonly ICurrentLandlordService _currentLandlordService;

        public ChatController(DataContext db, ICurrentLandlordService currentLandlordService)
        {
            _db = db;
            _currentLandlordService = currentLandlordService;
        }

        // GET: /Admin/Chat
        public async Task<IActionResult> Index()
        {
            var query = _db.ChatSessions.AsQueryable();

            if (!_currentLandlordService.IsSuperAdmin())
            {
                int currentLandlordId = _currentLandlordService.GetCurrentLandlordId() ?? 0;
                query = query.Where(s => s.LandlordId == currentLandlordId);
            }

            var sessions = await query
                .OrderByDescending(s => s.LastMsgAt)
                .Select(s => new
                {
                    s.SessionId,
                    s.GuestName,
                    s.GuestPhone,
                    s.LastMsgAt,
                    s.IsOpen,
                    s.TenantId,
                    s.UserId,
                    s.LandlordId,
                    UnreadCount = s.Messages.Count(m => !m.IsReadByAdmin && m.SenderType == ChatSenderType.Guest)
                })
                .ToListAsync();

            ViewBag.Sessions = sessions;
            return View();
        }

        // GET: /Admin/Chat/Messages?sessionId=5&after=0
        [HttpGet]
        public async Task<IActionResult> Messages(int sessionId, int after = 0)
        {
            var session = await _db.ChatSessions.AsNoTracking()
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);
            if (session == null) return Json(new { messages = Array.Empty<object>() });

            // Kiểm tra phân quyền: Chủ trọ chỉ xem tin nhắn của mình
            if (!_currentLandlordService.IsSuperAdmin())
            {
                int currentLandlordId = _currentLandlordService.GetCurrentLandlordId() ?? 0;
                if (session.LandlordId != currentLandlordId)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { messages = Array.Empty<object>(), error = "Không có quyền truy cập phiên chat của chủ trọ khác." });
                }
            }

            var msgs = await _db.ChatMessages.AsNoTracking()
                .Where(m => m.SessionId == sessionId && m.MessageId > after)
                .OrderBy(m => m.MessageId)
                .Select(m => new
                {
                    id         = m.MessageId,
                    content    = m.Content,
                    senderType = (int)m.SenderType,
                    createdAt  = m.CreatedAt.ToString("HH:mm dd/MM"),
                    isSystem   = m.SenderType == ChatSenderType.System
                })
                .ToListAsync();

            // Đánh dấu đã đọc cho tin nhắn từ khách
            var unread = await _db.ChatMessages
                .Where(m => m.SessionId == sessionId && !m.IsReadByAdmin && m.SenderType == ChatSenderType.Guest)
                .ToListAsync();
            if (unread.Any())
            {
                unread.ForEach(m => m.IsReadByAdmin = true);
                await _db.SaveChangesAsync();
            }

            return Json(new { messages = msgs });
        }

        // POST: /Admin/Chat/Send
        [HttpPost]
        public async Task<IActionResult> Send([FromBody] AdminSendRequest req)
        {
            if (req.SessionId <= 0 || string.IsNullOrWhiteSpace(req.Content))
                return Json(new { success = false });

            var session = await _db.ChatSessions.FirstOrDefaultAsync(s => s.SessionId == req.SessionId);
            if (session == null) return Json(new { success = false, message = "Phiên chat không tồn tại." });

            // Chống IDOR: Chủ trọ chỉ trả lời phiên của mình
            if (!_currentLandlordService.IsSuperAdmin())
            {
                int currentLandlordId = _currentLandlordService.GetCurrentLandlordId() ?? 0;
                if (session.LandlordId != currentLandlordId)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Bạn không có quyền gửi tin nhắn trong phiên chat này." });
                }
            }

            var msg = new tblChatMessage
            {
                SessionId     = req.SessionId,
                Content       = req.Content.Trim(),
                SenderType    = ChatSenderType.Admin,
                IsReadByAdmin = true,
                IsReadByGuest = false,
                CreatedAt     = DateTime.Now
            };
            _db.ChatMessages.Add(msg);

            session.LastMsgAt = DateTime.Now;
            await _db.SaveChangesAsync();

            return Json(new
            {
                success   = true,
                messageId = msg.MessageId,
                createdAt = msg.CreatedAt.ToString("HH:mm dd/MM")
            });
        }

        // POST: /Admin/Chat/Close
        [HttpPost]
        public async Task<IActionResult> Close(int sessionId)
        {
            var session = await _db.ChatSessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);
            if (session == null) return NotFound();

            if (!_currentLandlordService.IsSuperAdmin())
            {
                int currentLandlordId = _currentLandlordService.GetCurrentLandlordId() ?? 0;
                if (session.LandlordId != currentLandlordId)
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
            }

            session.IsOpen = false;
            await _db.SaveChangesAsync();
            return Ok();
        }
    }

    public sealed class AdminSendRequest
    {
        public int    SessionId { get; set; }
        public string Content   { get; set; } = string.Empty;
    }
}
