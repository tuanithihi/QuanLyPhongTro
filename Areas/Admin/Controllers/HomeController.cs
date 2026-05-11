using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Areas.Admin.Attributes;
using QuanLyPhongTro.Areas.Admin.Data;
using QuanLyPhongTro.Areas.Admin.Models;
using QuanLyPhongTro.Models;

namespace QuanLyPhongTro.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminOnly]
    public class HomeController : Controller
    {
        private readonly DataContext _context;

        public HomeController(DataContext context)
        {
            _context = context;
        }

        // GET: /Admin
        public async Task<IActionResult> Index()
        {
            var now = DateTime.Now;

            var vm = new DashboardViewModel
            {
                // ── Phòng ────────────────────────────────────────────
                TotalRooms       = await _context.Rooms.CountAsync(),
                AvailableRooms   = await _context.Rooms.CountAsync(r => r.Status == RoomStatus.Available),
                OccupiedRooms    = await _context.Rooms.CountAsync(r => r.Status == RoomStatus.Occupied),
                MaintenanceRooms = await _context.Rooms.CountAsync(r => r.Status == RoomStatus.Maintenance),

                // ── Hợp đồng ─────────────────────────────────────────
                ActiveContracts = await _context.Contracts
                    .CountAsync(c => c.Status == ContractStatus.Active),

                ExpiringContractsIn30Days = await _context.Contracts
                    .CountAsync(c => c.Status == ContractStatus.Active
                                  && c.EndDate.HasValue
                                  && c.EndDate <= DateTime.Now.AddDays(30)),

                // ── Người thuê ────────────────────────────────────────
                TotalTenants = await _context.Tenants.CountAsync(t => t.IsActive),

                // ── Tài chính tháng hiện tại ──────────────────────────
                TotalRevenueThisMonth = await _context.Invoices
                    .Where(i => i.Status == InvoiceStatus.Paid
                             && i.BillingMonth == now.Month
                             && i.BillingYear  == now.Year)
                    .SumAsync(i => (decimal?)i.TotalAmount) ?? 0,

                PaidInvoicesThisMonth = await _context.Invoices
                    .CountAsync(i => i.Status == InvoiceStatus.Paid
                                  && i.BillingMonth == now.Month
                                  && i.BillingYear  == now.Year),

                UnpaidInvoicesThisMonth = await _context.Invoices
                    .CountAsync(i => i.Status == InvoiceStatus.Unpaid
                                  && i.BillingMonth == now.Month
                                  && i.BillingYear  == now.Year),

                OverdueInvoicesThisMonth = await _context.Invoices
                    .CountAsync(i => i.Status == InvoiceStatus.Overdue
                                  && i.BillingMonth == now.Month
                                  && i.BillingYear  == now.Year),

                // ── Yêu cầu chờ xử lý ────────────────────────────────
                PendingBookingRequests = await _context.BookingRequests
                    .CountAsync(b => b.Status == BookingRequestStatus.Pending),

                // ── Chat có tin nhắn khách chưa đọc ─────────────────────
                OpenChatSessions = await _context.ChatSessions
                    .CountAsync(s => s.Messages.Any(m => m.SenderType == ChatSenderType.Guest
                                                      && !m.IsReadByAdmin)),
            };

            return View(vm);
        }
    }
}
