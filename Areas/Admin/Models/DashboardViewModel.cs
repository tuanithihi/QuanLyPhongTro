namespace QuanLyPhongTro.Areas.Admin.Models
{
    /// <summary>
    /// Dữ liệu tổng hợp hiển thị trên trang Dashboard.
    /// </summary>
    public class DashboardViewModel
    {
        // Thống kê phòng
        public int TotalRooms { get; set; }
        public int AvailableRooms { get; set; }
        public int OccupiedRooms { get; set; }
        public int MaintenanceRooms { get; set; }

        // Thống kê tài chính tháng hiện tại
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

        // Hợp đồng
        public int ActiveContracts { get; set; }
        public int ExpiringContractsIn30Days { get; set; }

        // Người thuê
        public int TotalTenants { get; set; }

        // Yêu cầu chờ xử lý
        public int PendingBookingRequests { get; set; }

        // Phiên chat đang mở (chờ phản hồi)
        public int OpenChatSessions { get; set; }
    }
}
