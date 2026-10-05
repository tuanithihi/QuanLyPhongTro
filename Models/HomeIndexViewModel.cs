using QuanLyPhongTro.Models.ViewModels;

namespace QuanLyPhongTro.Models
{
    /// <summary>
    /// ViewModel cho trang chủ người dùng — tìm kiếm nhanh, phòng nổi bật, khu vực, chủ trọ uy tín.
    /// </summary>
    public class HomeIndexViewModel
    {
        // ── Dữ liệu hiển thị mới (Card ViewModels) ───────────────────────
        public List<RoomCardViewModel> FeaturedCardRooms { get; set; } = new();
        public List<RoomCardViewModel> RecentCardRooms   { get; set; } = new();
        public List<tblProvince>       PopularProvinces  { get; set; } = new();
        public List<tblLandlord>       FeaturedLandlords { get; set; } = new();

        // ── Dữ liệu tương thích cũ ──────────────────────────────────────
        public List<tblRoom>     AvailableRooms { get; set; } = new();
        public List<tblRoom>     FeaturedRooms  { get; set; } = new();
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
