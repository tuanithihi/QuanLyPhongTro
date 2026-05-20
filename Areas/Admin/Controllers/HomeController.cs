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
        public async Task<IActionResult> Index(string period = "month", int? month = null, int? quarter = null,
                                               int? year = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var now = DateTime.Now;
            var filter = ResolveDashboardPeriod(period, month, quarter, year, fromDate, toDate);

            var invoiceQuery = _context.Invoices.AsQueryable();
            if (filter.UseBillingPeriod)
            {
                var startKey = filter.Start.Year * 100 + filter.Start.Month;
                var endKey = filter.End.Year * 100 + filter.End.Month;
                invoiceQuery = invoiceQuery.Where(i =>
                    (i.BillingYear * 100 + i.BillingMonth) >= startKey &&
                    (i.BillingYear * 100 + i.BillingMonth) <= endKey);
            }
            else
            {
                var endExclusive = filter.End.Date.AddDays(1);
                invoiceQuery = invoiceQuery.Where(i => i.DueDate >= filter.Start.Date && i.DueDate < endExclusive);
            }

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

                // ── Tài chính theo khoảng thời gian đang chọn ──────────
                TotalRevenueThisMonth = await invoiceQuery
                    .Where(i => i.Status == InvoiceStatus.Paid)
                    .SumAsync(i => (decimal?)i.TotalAmount) ?? 0,

                ExpectedRevenueInPeriod = await invoiceQuery
                    .SumAsync(i => (decimal?)i.TotalAmount) ?? 0,

                TotalInvoicesInPeriod = await invoiceQuery.CountAsync(),

                PaidInvoicesThisMonth = await invoiceQuery
                    .CountAsync(i => i.Status == InvoiceStatus.Paid),

                UnpaidInvoicesThisMonth = await invoiceQuery
                    .CountAsync(i => i.Status == InvoiceStatus.Unpaid),

                OverdueInvoicesThisMonth = await invoiceQuery
                    .CountAsync(i => i.Status == InvoiceStatus.Overdue),

                PeriodType = filter.Type,
                PeriodLabel = filter.Label,
                PeriodStart = filter.Start,
                PeriodEnd = filter.End,
                SelectedMonth = filter.Month,
                SelectedQuarter = filter.Quarter,
                SelectedYear = filter.Year,

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

        private static DashboardPeriodFilter ResolveDashboardPeriod(string period, int? month, int? quarter,
                                                                    int? year, DateTime? fromDate, DateTime? toDate)
        {
            var today = DateTime.Today;
            var selectedYear = Math.Clamp(year ?? today.Year, 2000, 2100);
            var type = (period ?? "month").Trim().ToLowerInvariant();

            if (type == "year")
            {
                var start = new DateTime(selectedYear, 1, 1);
                var end = new DateTime(selectedYear, 12, 31);
                return new DashboardPeriodFilter(type, start, end, $"Năm {selectedYear}", 1, 1, selectedYear, true);
            }

            if (type == "quarter")
            {
                var selectedQuarter = Math.Clamp(quarter ?? ((today.Month - 1) / 3 + 1), 1, 4);
                var startMonth = (selectedQuarter - 1) * 3 + 1;
                var start = new DateTime(selectedYear, startMonth, 1);
                var endMonth = startMonth + 2;
                var end = new DateTime(selectedYear, endMonth, DateTime.DaysInMonth(selectedYear, endMonth));
                return new DashboardPeriodFilter(type, start, end, $"Quý {selectedQuarter}/{selectedYear}",
                    startMonth, selectedQuarter, selectedYear, true);
            }

            if (type == "custom")
            {
                var start = (fromDate ?? new DateTime(today.Year, today.Month, 1)).Date;
                var end = (toDate ?? today).Date;
                if (end < start)
                    (start, end) = (end, start);

                return new DashboardPeriodFilter(type, start, end,
                    $"{start:dd/MM/yyyy} - {end:dd/MM/yyyy}", start.Month, ((start.Month - 1) / 3) + 1, start.Year, false);
            }

            var selectedMonth = Math.Clamp(month ?? today.Month, 1, 12);
            var monthStart = new DateTime(selectedYear, selectedMonth, 1);
            var monthEnd = new DateTime(selectedYear, selectedMonth, DateTime.DaysInMonth(selectedYear, selectedMonth));
            return new DashboardPeriodFilter("month", monthStart, monthEnd, $"Tháng {selectedMonth}/{selectedYear}",
                selectedMonth, ((selectedMonth - 1) / 3) + 1, selectedYear, true);
        }

        private sealed record DashboardPeriodFilter(string Type, DateTime Start, DateTime End, string Label,
                                                    int Month, int Quarter, int Year, bool UseBillingPeriod);
    }
}
