using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Areas.Admin.Data;
using QuanLyPhongTro.Models;

namespace QuanLyPhongTro.Controllers
{
    /// <summary>
    /// Xử lý chat phía khách (guest) — không cần đăng nhập.
    /// </summary>
    public class ChatController : Controller
    {
        private readonly DataContext _db;
        private const string CookieName = "chat_session";

        public ChatController(DataContext db)
        {
            _db = db;
        }

        // ─────────────────────────────────────────────────────────────────
        // POST /Chat/Start
        // Bắt đầu hoặc tiếp tục phiên chat. Trả về { sessionKey, sessionId }.
        // · Nếu đang đăng nhập (Tenant/User): tự lấy tên/SĐT từ DB, tìm lại phiên cũ theo tài khoản.
        // · Nếu ẩn danh: kiểm tra cookie, yêu cầu nhập tên/SĐT.
        // ─────────────────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Start([FromBody] StartChatRequest req)
        {
            // ── Nhận dạng tài khoản đang đăng nhập (server-side session) ──
            int? tenantId = int.TryParse(HttpContext.Session.GetString("TenantUser"), out var tid) ? tid : null;
            int? userId   = int.TryParse(HttpContext.Session.GetString("NormalUser"), out var uid) ? uid : null;

            string resolvedName  = req.GuestName?.Trim()  ?? "";
            string resolvedPhone = req.GuestPhone?.Trim() ?? "";

            // ── Tenant đã đăng nhập ────────────────────────────────────────
            if (tenantId.HasValue)
            {
                var tenant = await _db.Tenants.FindAsync(tenantId.Value);
                if (tenant != null)
                {
                    resolvedName  = tenant.FullName;
                    resolvedPhone = tenant.Phone ?? resolvedPhone;
                }

                // Tìm phiên đang mở của tenant này
                var existing = await _db.ChatSessions
                    .Where(s => s.TenantId == tenantId && s.IsOpen)
                    .OrderByDescending(s => s.LastMsgAt)
                    .FirstOrDefaultAsync();

                if (existing != null)
                {
                    SetCookie(existing.SessionKey);
                    return Json(new { success = true, sessionKey = existing.SessionKey, sessionId = existing.SessionId });
                }
            }
            // ── Người dùng website đã đăng nhập ───────────────────────────
            else if (userId.HasValue)
            {
                var user = await _db.Users.FindAsync(userId.Value);
                if (user != null)
                {
                    resolvedName  = string.IsNullOrEmpty(user.FullName) ? user.Username : user.FullName;
                    resolvedPhone = user.Phone ?? resolvedPhone;
                }

                var existing = await _db.ChatSessions
                    .Where(s => s.UserId == userId && s.IsOpen)
                    .OrderByDescending(s => s.LastMsgAt)
                    .FirstOrDefaultAsync();

                if (existing != null)
                {
                    SetCookie(existing.SessionKey);
                    return Json(new { success = true, sessionKey = existing.SessionKey, sessionId = existing.SessionId });
                }
            }
            // ── Khách ẩn danh ──────────────────────────────────────────────
            else
            {
                var existingKey = Request.Cookies[CookieName];
                if (!string.IsNullOrEmpty(existingKey))
                {
                    var existing = await _db.ChatSessions.FirstOrDefaultAsync(s => s.SessionKey == existingKey && s.IsOpen);
                    if (existing != null)
                        return Json(new { success = true, sessionKey = existing.SessionKey, sessionId = existing.SessionId });
                }

                if (string.IsNullOrWhiteSpace(resolvedName) || string.IsNullOrWhiteSpace(resolvedPhone))
                    return Json(new { success = false, message = "Vui lòng nhập họ tên và số điện thoại." });
            }

            // ── Tạo phiên mới ──────────────────────────────────────────────
            var session = new tblChatSession
            {
                SessionKey = Guid.NewGuid().ToString(),
                GuestName  = string.IsNullOrEmpty(resolvedName) ? "Khách" : resolvedName,
                GuestPhone = resolvedPhone,
                TenantId   = tenantId,
                UserId     = userId,
                CreatedAt  = DateTime.Now,
                LastMsgAt  = DateTime.Now
            };
            _db.ChatSessions.Add(session);
            await _db.SaveChangesAsync();

            SetCookie(session.SessionKey);
            return Json(new { success = true, sessionKey = session.SessionKey, sessionId = session.SessionId });

            void SetCookie(string key) => Response.Cookies.Append(CookieName, key, new CookieOptions
            {
                Expires  = DateTimeOffset.UtcNow.AddDays(30),
                HttpOnly = true,
                SameSite = SameSiteMode.Lax
            });
        }

        // ─────────────────────────────────────────────────────────────────
        // POST /Chat/Send
        // Gửi tin nhắn từ khách.
        // ─────────────────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Send([FromBody] SendChatRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.SessionKey) || string.IsNullOrWhiteSpace(req.Content))
                return Json(new { success = false });

            var session = await _db.ChatSessions.FirstOrDefaultAsync(s => s.SessionKey == req.SessionKey);
            if (session == null) return Json(new { success = false, message = "Phiên chat không hợp lệ." });

            var msg = new tblChatMessage
            {
                SessionId    = session.SessionId,
                Content      = req.Content.Trim(),
                SenderType   = ChatSenderType.Guest,
                IsReadByAdmin = false,
                IsReadByGuest = true,
                CreatedAt    = DateTime.Now
            };
            _db.ChatMessages.Add(msg);

            session.LastMsgAt = DateTime.Now;
            await _db.SaveChangesAsync();

            return Json(new
            {
                success   = true,
                messageId = msg.MessageId,
                createdAt = msg.CreatedAt.ToString("HH:mm")
            });
        }

        // ─────────────────────────────────────────────────────────────────
        // GET /Chat/Poll?sessionKey=xxx&after=123
        // Lấy tin nhắn mới hơn messageId = after (polling 4s).
        // ─────────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Poll(string sessionKey, int after = 0)
        {
            var session = await _db.ChatSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SessionKey == sessionKey);
            if (session == null) return Json(new { messages = Array.Empty<object>() });

            var msgs = await _db.ChatMessages
                .AsNoTracking()
                .Where(m => m.SessionId == session.SessionId && m.MessageId > after)
                .OrderBy(m => m.MessageId)
                .Select(m => new
                {
                    id         = m.MessageId,
                    content    = m.Content,
                    senderType = (int)m.SenderType,
                    createdAt  = m.CreatedAt.ToString("HH:mm"),
                    isSystem   = m.SenderType == ChatSenderType.System
                })
                .ToListAsync();

            // Đánh dấu đã đọc cho tin nhắn từ admin/system
            var unread = await _db.ChatMessages
                .Where(m => m.SessionId == session.SessionId
                         && m.MessageId > after
                         && !m.IsReadByGuest
                         && (m.SenderType == ChatSenderType.Admin || m.SenderType == ChatSenderType.System))
                .ToListAsync();
            if (unread.Any())
            {
                unread.ForEach(m => m.IsReadByGuest = true);
                await _db.SaveChangesAsync();
            }

            return Json(new { messages = msgs });
        }
    }

    // ── DTOs ──────────────────────────────────────────────────────────────
    public sealed class StartChatRequest
    {
        public string GuestName  { get; set; } = string.Empty;
        public string GuestPhone { get; set; } = string.Empty;
    }

    public sealed class SendChatRequest
    {
        public string SessionKey { get; set; } = string.Empty;
        public string Content    { get; set; } = string.Empty;
    }
}
