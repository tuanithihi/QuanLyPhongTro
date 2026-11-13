using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Areas.Admin.Data;
using QuanLyPhongTro.Models;

namespace QuanLyPhongTro.Controllers
{
    public class ContactController : Controller
    {
        private readonly DataContext _db;

        public ContactController(DataContext db)
        {
            _db = db;
        }

        // GET: /Contact hoặc /lien-he
        [HttpGet("/Contact")]
        [HttpGet("/lien-he")]
        public async Task<IActionResult> Index()
        {
            var model = new ContactViewModel();

            int? tenantId = null;
            int? userId   = null;
            try
            {
                if (HttpContext?.Session != null)
                {
                    tenantId = int.TryParse(HttpContext.Session.GetString("TenantUser"), out var tid) ? tid : null;
                    userId   = int.TryParse(HttpContext.Session.GetString("NormalUser"), out var uid) ? uid : null;
                }
            }
            catch (InvalidOperationException)
            {
                // Bỏ qua nếu Session chưa cấu hình (vd môi trường kiểm thử)
            }

            if (tenantId.HasValue)
            {
                var tenant = await _db.Tenants.FindAsync(tenantId.Value);
                if (tenant != null)
                {
                    model.FullName = tenant.FullName;
                    model.Phone = tenant.Phone ?? "";
                    model.Email = tenant.Email;
                }
            }
            else if (userId.HasValue)
            {
                var user = await _db.Users.FindAsync(userId.Value);
                if (user != null)
                {
                    model.FullName = !string.IsNullOrEmpty(user.FullName) ? user.FullName : user.Username;
                    model.Phone = user.Phone ?? "";
                    model.Email = user.Email;
                }
            }

            return View(model);
        }

        // POST: /Contact hoặc /lien-he
        [HttpPost("/Contact")]
        [HttpPost("/lien-he")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ContactViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Lưu yêu cầu liên hệ vào hệ thống BookingRequests (loại ChatMessage)
                // để Quản trị viên/Chủ trọ có thể theo dõi và phản hồi trong Admin
                var firstRoom = await _db.Rooms.OrderBy(r => r.RoomId).FirstOrDefaultAsync();
                int fallbackRoomId = firstRoom?.RoomId ?? 1;

                var req = new tblBookingRequest
                {
                    RoomId = fallbackRoomId,
                    FullName = model.FullName.Trim(),
                    Phone = model.Phone.Trim(),
                    Email = model.Email?.Trim(),
                    PreferredDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                    Message = $"[LIÊN HỆ WEBSITE - {model.Subject.Trim()}]: {model.Message.Trim()}",
                    RequestType = BookingRequestType.ChatMessage,
                    Status = BookingRequestStatus.Pending
                };

                _db.BookingRequests.Add(req);
                await _db.SaveChangesAsync();

                TempData["Success"] = "Cảm ơn bạn đã gửi liên hệ! Đội ngũ hỗ trợ của Z-Stay đã tiếp nhận thông tin và sẽ phản hồi sớm nhất qua số điện thoại hoặc email của bạn.";
                return RedirectToAction("Index");
            }
            catch
            {
                TempData["Error"] = "Có lỗi xảy ra trong quá trình gửi tin nhắn liên hệ. Vui lòng thử lại hoặc gọi trực tiếp đến Hotline 1900 8888.";
                return View(model);
            }
        }
    }
}