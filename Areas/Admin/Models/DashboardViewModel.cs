namespace QuanLyPhongTro.Areas.Admin.Models
{
    public class LocationStatItem
    {
        public string ProvinceName { get; set; } = string.Empty;
        public int RoomCount { get; set; }
        public int PropertyCount { get; set; }
    }

    /// <summary>
    /// Dữ liệu tổng hợp hiển thị trên trang Dashboard.
    /// </summary>
    public class DashboardViewModel
    {
        public bool IsSuperAdmin { get; set; }

        // ── Thống kê SuperAdmin ──────────────────────────────────────────
        public int TotalLandlords { get; set; }
        public int PendingLandlords { get; set; }
        public int PendingRooms { get; set; }
        public int TotalRoomViews { get; set; }
        public int NewUsersThisWeek { get; set; }
        public int NewUsersThisMonth { get; set; }
        public List<LocationStatItem> RoomsByProvince { get; set; } = new();

        // ── Thống kê phòng & Vận hành (chung hoặc theo chủ trọ) ──────────
        public int TotalRooms { get; set; }
        public int AvailableRooms { get; set; }
        public int OccupiedRooms { get; set; }
        public int MaintenanceRooms { get; set; }
        public double OccupancyRate { get; set; }

        // ── Thống kê tài chính theo khoảng thời gian ──────────────────────
        public decimal TotalRevenueThisMonth { get; set; }
        public int PaidInvoicesThisMonth { get; set; }
        public int UnpaidInvoicesThisMonth { get; set; }
        public int OverdueInvoicesThisMonth { get; set; }
        public decimal ExpectedRevenueInPeriod { get; set; }
        public int TotalInvoicesInPeriod { get; set; }
        public string PeriodType { get; set; } = "month";
        public string PeriodLabel { get; set; } = string.Empty;
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public int SelectedMonth { get; set; }
        public int SelectedQuarter { get; set; }
        public int SelectedYear { get; set; }

        // ── Hợp đồng ─────────────────────────────────────────────────────
        public int ActiveContracts { get; set; }
        public int ExpiringContractsIn30Days { get; set; }

        // ── Người thuê ────────────────────────────────────────────────────
        public int TotalTenants { get; set; }

        // ── Yêu cầu & Tương tác ───────────────────────────────────────────
        public int PendingBookingRequests { get; set; }
        public int OpenChatSessions { get; set; }
    }
}
