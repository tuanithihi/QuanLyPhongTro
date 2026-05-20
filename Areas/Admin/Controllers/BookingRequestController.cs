using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Areas.Admin.Attributes;
using QuanLyPhongTro.Areas.Admin.Data;
using QuanLyPhongTro.Models;

namespace QuanLyPhongTro.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminOnly]
    public class BookingRequestController : Controller
    {
        private readonly DataContext _context;

        public BookingRequestController(DataContext context)
        {
            _context = context;
        }

        // GET: /Admin/BookingRequest
        public async Task<IActionResult> Index(string? type, string? status, int page = 1)
        {
            const int pageSize = 15;

            var query = _context.BookingRequests
                .Include(b => b.Room)
                .Where(b => b.RequestType == BookingRequestType.ViewingRequest)
                .AsQueryable();

            // ── Lọc theo trạng thái ────────────────────────────────────
            if (status == "pending")
                query = query.Where(b => b.Status == BookingRequestStatus.Pending);
            else if (status == "accepted")
                query = query.Where(b => b.Status == BookingRequestStatus.Accepted);
            else if (status == "rejected")
                query = query.Where(b => b.Status == BookingRequestStatus.Rejected);

            int total = await query.CountAsync();

            var items = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Page        = page;
            ViewBag.PageSize    = pageSize;
            ViewBag.TotalItems  = total;
            ViewBag.TotalPages  = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.Type        = type ?? "";
            ViewBag.Status      = status ?? "";
            ViewBag.PendingCount= await _context.BookingRequests.CountAsync(b => b.Status == BookingRequestStatus.Pending);

            return View(items);
        }

        // POST: /Admin/BookingRequest/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string action, string? adminNote,
                                                       string? returnType, string? returnStatus, int returnPage = 1)
        {
            var req = await _context.BookingRequests.Include(b => b.Room).FirstOrDefaultAsync(b => b.RequestId == id);
            if (req == null) return NotFound();

            req.Status    = action == "accept" ? BookingRequestStatus.Accepted : BookingRequestStatus.Rejected;
            req.AdminNote = adminNote?.Trim();
            req.IsGuestNotified = false;
            await _context.SaveChangesAsync();

            // ── Gửi thông báo hệ thống tới phiên chat của khách (nếu có) ──
            var chatSession = await _context.ChatSessions
                .Where(s => s.GuestPhone == req.Phone && s.IsOpen)
                .OrderByDescending(s => s.LastMsgAt)
                .FirstOrDefaultAsync();

            if (chatSession != null)
            {
                string roomName = req.Room?.RoomName ?? $"(ID {req.RoomId})";
                string sysMsg = action == "accept"
                    ? $"✅ Yêu cầu đặt lịch xem phòng \"{roomName}\" của bạn đã được XÁC NHẬN."
                      + (req.AdminNote != null ? $" Ghi chú: {req.AdminNote}" : "")
                      + " Chúng tôi sẽ liên hệ để xác nhận chi tiết sớm nhất."
                    : $"❌ Yêu cầu đặt lịch xem phòng \"{roomName}\" của bạn đã bị TỪ CHỐI."
                      + (req.AdminNote != null ? $" Lý do: {req.AdminNote}" : "")
                      + " Vui lòng liên hệ lại để biết thêm thông tin.";

                // Tin nhắn hệ thống trong chat
                _context.ChatMessages.Add(new tblChatMessage
                {
                    SessionId     = chatSession.SessionId,
                    Content       = sysMsg,
                    SenderType    = ChatSenderType.System,
                    IsReadByAdmin = true,
                    IsReadByGuest = false,
                    CreatedAt     = DateTime.Now
                });

                chatSession.LastMsgAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = action == "accept"
                ? "Đã chấp nhận yêu cầu."
                : "Đã từ chối yêu cầu.";

            return RedirectToAction("Index", new { type = returnType, status = returnStatus, page = returnPage });
        }

        // POST: /Admin/BookingRequest/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, string? returnType, string? returnStatus, int returnPage = 1)
        {
            var req = await _context.BookingRequests.FindAsync(id);
            if (req != null)
            {
                _context.BookingRequests.Remove(req);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa yêu cầu.";
            }
            return RedirectToAction("Index", new { type = returnType, status = returnStatus, page = returnPage });
        }
    }
}
