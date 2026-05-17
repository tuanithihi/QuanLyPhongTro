namespace QuanLyPhongTro.Models
{
    /// <summary>
    /// ViewModel cho trang chủ người dùng — danh sách phòng trống + bộ lọc.
    /// </summary>
    public class HomeIndexViewModel
    {
        // ── Dữ liệu hiển thị ─────────────────────────────────────────────
        public List<tblRoom>     AvailableRooms { get; set; } = new(); // Search results
        public List<tblRoom>     FeaturedRooms  { get; set; } = new(); // Featured/Top rooms
        public List<tblRoomType> RoomTypes      { get; set; } = new();
        public List<tblPost>     RecentPosts    { get; set; } = new();
        public List<tblReview>   RecentReviews  { get; set; } = new();

        // ── Tham số tìm kiếm / lọc (bind từ query string) ────────────────
        public string? Area       { get; set; }
        public int?    RoomTypeId { get; set; }
        public string? PriceRange { get; set; }
        public string? AreaRange  { get; set; }
        public string? FloorRange { get; set; }

        // ── Vị trí người dùng (để tính khoảng cách) ─────────────────────
        public double? UserLat { get; set; }
        public double? UserLng { get; set; }
    }
}
