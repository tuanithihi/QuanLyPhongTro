using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Areas.Admin.Attributes;
using QuanLyPhongTro.Areas.Admin.Data;
using QuanLyPhongTro.Models;

namespace QuanLyPhongTro.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminOnly]
    public class ChatController : Controller
    {
        private readonly DataContext _db;

        public ChatController(DataContext db)
        {
            _db = db;
        }

        // GET: /Admin/Chat
        public async Task<IActionResult> Index()
        {
            var sessions = await _db.ChatSessions
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

            // Mark guest messages as read by admin
            var unread = await _db.ChatMessages
                .Where(m => m.SessionId == sessionId
                         && m.SenderType == ChatSenderType.Guest
                         && !m.IsReadByAdmin)
                .ToListAsync();
            if (unread.Any())
            {
                unread.ForEach(m => m.IsReadByAdmin = true);
                await _db.SaveChangesAsync();
            }

            return Json(new
            {
                messages    = msgs,
                guestName   = session.GuestName,
                guestPhone  = session.GuestPhone
            });
        }

        // POST: /Admin/Chat/Reply
        [HttpPost]
        public async Task<IActionResult> Reply([FromBody] AdminReplyRequest req)
        {
            if (req.SessionId <= 0 || string.IsNullOrWhiteSpace(req.Content))
                return Json(new { success = false });

            var session = await _db.ChatSessions.FindAsync(req.SessionId);
            if (session == null) return Json(new { success = false });

            var msg = new tblChatMessage
            {
                SessionId    = req.SessionId,
                Content      = req.Content.Trim(),
                SenderType   = ChatSenderType.Admin,
                IsReadByAdmin = true,
                IsReadByGuest = false,
                CreatedAt    = DateTime.Now
            };
            _db.ChatMessages.Add(msg);
            session.LastMsgAt = DateTime.Now;
            await _db.SaveChangesAsync();

            return Json(new { success = true, messageId = msg.MessageId, createdAt = msg.CreatedAt.ToString("HH:mm dd/MM") });
        }

        // GET: /Admin/Chat/UnreadCount — sidebar badge polling
        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            int count = await _db.ChatMessages
                .CountAsync(m => !m.IsReadByAdmin && m.SenderType == ChatSenderType.Guest);
            return Json(new { count });
        }
    }

    public sealed class AdminReplyRequest
    {
        public int    SessionId { get; set; }
        public string Content   { get; set; } = string.Empty;
    }
}
