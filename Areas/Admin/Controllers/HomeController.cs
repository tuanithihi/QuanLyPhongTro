using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Areas.Admin.Attributes;
using QuanLyPhongTro.Areas.Admin.Data;
using QuanLyPhongTro.Areas.Admin.Models;
using QuanLyPhongTro.Models;
using QuanLyPhongTro.Services;

namespace QuanLyPhongTro.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminOnly]
    public class HomeController : Controller
    {
        private readonly DataContext _context;
        private readonly ICurrentLandlordService _currentLandlordService;

        public HomeController(DataContext context, ICurrentLandlordService currentLandlordService)
        {
            _context = context;
            _currentLandlordService = currentLandlordService;
        }

        // GET: /Admin
        public async Task<IActionResult> Index(string period = "month", int? month = null, int? quarter = null,
                                               int? year = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var now = DateTime.Now;
            var filter = ResolveDashboardPeriod(period, month, quarter, year, fromDate, toDate);
            bool isSuperAdmin = _currentLandlordService.IsSuperAdmin();
            if (!isSuperAdmin)
            {
                var landlord = await _currentLandlordService.GetCurrentLandlordAsync();
                if (landlord != null && landlord.Status == LandlordStatus.Pending)
                {
                    return RedirectToAction("LandlordStatus", "Account", new { area = "" });
                }
            }
            int? landlordId = isSuperAdmin ? null : _currentLandlordService.GetCurrentLandlordId();

            var vm = new DashboardViewModel
            {
                IsSuperAdmin = isSuperAdmin,
                PeriodType = filter.Type,
                PeriodLabel = filter.Label,
                PeriodStart = filter.Start,
                PeriodEnd = filter.End,
                SelectedMonth = filter.Month,
                SelectedQuarter = filter.Quarter,
                SelectedYear = filter.Year,
            };

            // ── INVOICES QUERY ────────────────────────────────────────────
            var invoiceQuery = _context.Invoices.AsNoTracking().AsQueryable();
            if (!isSuperAdmin && landlordId.HasValue)
            {
                invoiceQuery = invoiceQuery.Where(i => i.LandlordId == landlordId.Value);
            }

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

            vm.TotalRevenueThisMonth = await invoiceQuery
                .Where(i => i.Status == InvoiceStatus.Paid)
                .SumAsync(i => (decimal?)i.TotalAmount) ?? 0;
            vm.ExpectedRevenueInPeriod = await invoiceQuery
                .SumAsync(i => (decimal?)i.TotalAmount) ?? 0;
            vm.TotalInvoicesInPeriod = await invoiceQuery.CountAsync();
            vm.PaidInvoicesThisMonth = await invoiceQuery
                .CountAsync(i => i.Status == InvoiceStatus.Paid);
            vm.UnpaidInvoicesThisMonth = await invoiceQuery
                .CountAsync(i => i.Status == InvoiceStatus.Unpaid);
            vm.OverdueInvoicesThisMonth = await invoiceQuery
                .CountAsync(i => i.Status == InvoiceStatus.Overdue);

            if (isSuperAdmin)
            {
                // ── SUPERADMIN METRICS ────────────────────────────────────
                vm.TotalLandlords = await _context.Landlords.CountAsync();
                vm.PendingLandlords = await _context.Landlords.CountAsync(l => l.Status == LandlordStatus.Pending);
                vm.PendingRooms = await _context.Rooms.CountAsync(r => r.ApprovalStatus == RoomApprovalStatus.Pending);
                vm.TotalRoomViews = await _context.Rooms.SumAsync(r => (int?)r.ViewCount) ?? 0;

                var weekAgo = now.AddDays(-7);
                var monthAgo = now.AddDays(-30);
                vm.NewUsersThisWeek = await _context.Users.CountAsync(u => u.CreatedAt >= weekAgo);
                vm.NewUsersThisMonth = await _context.Users.CountAsync(u => u.CreatedAt >= monthAgo);

                // Biểu đồ khu vực theo Tỉnh/Thành
                vm.RoomsByProvince = await _context.Properties
                    .AsNoTracking()
                    .Where(p => p.Province != null)
                    .GroupBy(p => p.Province!.Name)
                    .Select(g => new LocationStatItem
                    {
                        ProvinceName = g.Key,
                        PropertyCount = g.Count(),
                        RoomCount = g.SelectMany(p => p.Rooms).Count()
                    })
                    .OrderByDescending(x => x.RoomCount)
                    .Take(8)
                    .ToListAsync();

                // Tổng phòng toàn hệ thống
                vm.TotalRooms = await _context.Rooms.CountAsync();
                vm.AvailableRooms = await _context.Rooms.CountAsync(r => r.Status == RoomStatus.Available);
                vm.OccupiedRooms = await _context.Rooms.CountAsync(r => r.Status == RoomStatus.Occupied);
                vm.MaintenanceRooms = await _context.Rooms.CountAsync(r => r.Status == RoomStatus.Maintenance);

                // Hợp đồng toàn hệ thống
                vm.ActiveContracts = await _context.Contracts
                    .CountAsync(c => c.Status == ContractStatus.Active);
                vm.ExpiringContractsIn30Days = await _context.Contracts
                    .CountAsync(c => c.Status == ContractStatus.Active
                                  && c.EndDate.HasValue
                                  && c.EndDate <= now.AddDays(30));

                // Người thuê toàn hệ thống
                vm.TotalTenants = await _context.Tenants.CountAsync(t => t.IsActive);

                // Yêu cầu và Chat toàn hệ thống
                vm.PendingBookingRequests = await _context.BookingRequests
                    .CountAsync(b => b.Status == BookingRequestStatus.Pending);
                vm.OpenChatSessions = await _context.ChatSessions
                    .CountAsync(s => s.Messages.Any(m => m.SenderType == ChatSenderType.Guest && !m.IsReadByAdmin));
            }
            else
            {
                // ── LANDLORD METRICS (Chỉ tính dữ liệu của chính chủ trọ đó) ──
                int lid = landlordId ?? 0;

                var roomQuery = _context.Rooms
                    .AsNoTracking()
                    .Where(r => r.Property != null && r.Property.LandlordId == lid);

                vm.TotalRooms = await roomQuery.CountAsync();
                vm.AvailableRooms = await roomQuery.CountAsync(r => r.Status == RoomStatus.Available);
                vm.OccupiedRooms = await roomQuery.CountAsync(r => r.Status == RoomStatus.Occupied);
                vm.MaintenanceRooms = await roomQuery.CountAsync(r => r.Status == RoomStatus.Maintenance);
                vm.PendingRooms = await roomQuery.CountAsync(r => r.ApprovalStatus == RoomApprovalStatus.Pending);
                vm.TotalRoomViews = await roomQuery.SumAsync(r => (int?)r.ViewCount) ?? 0;

                // Hợp đồng của chủ trọ
                vm.ActiveContracts = await _context.Contracts
                    .CountAsync(c => c.LandlordId == lid && c.Status == ContractStatus.Active);
                vm.ExpiringContractsIn30Days = await _context.Contracts
                    .CountAsync(c => c.LandlordId == lid
                                  && c.Status == ContractStatus.Active
                                  && c.EndDate.HasValue
                                  && c.EndDate <= now.AddDays(30));

                // Người thuê của chủ trọ
                vm.TotalTenants = await _context.Tenants
                    .CountAsync(t => t.LandlordId == lid && t.IsActive);

                // Yêu cầu đặt lịch & Chat của chủ trọ
                vm.PendingBookingRequests = await _context.BookingRequests
                    .CountAsync(b => b.LandlordId == lid && b.Status == BookingRequestStatus.Pending);
                vm.OpenChatSessions = await _context.ChatSessions
                    .CountAsync(s => s.LandlordId == lid && s.Messages.Any(m => m.SenderType == ChatSenderType.Guest && !m.IsReadByAdmin));
            }

            vm.OccupancyRate = vm.TotalRooms > 0
                ? Math.Round(vm.OccupiedRooms * 100.0 / vm.TotalRooms, 1)
                : 0.0;

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
