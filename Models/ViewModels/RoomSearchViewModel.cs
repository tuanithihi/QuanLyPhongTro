using System.ComponentModel.DataAnnotations;
using QuanLyPhongTro.Models;

namespace QuanLyPhongTro.Models.ViewModels
{
    public class RoomFilterCriteria
    {
        public string? Keyword { get; set; }

        public int? ProvinceId { get; set; }
        public string? ProvinceSlug { get; set; }

        public int? DistrictId { get; set; }
        public string? DistrictSlug { get; set; }

        public int? WardId { get; set; }

        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }

        public double? MinArea { get; set; }
        public double? MaxArea { get; set; }

        public int? RoomTypeId { get; set; }

        public List<int>? AmenityIds { get; set; } = new List<int>();

        /// <summary>
        /// newest, price_asc, price_desc, area_desc
        /// </summary>
        public string? SortBy { get; set; } = "newest";

        /// <summary>
        /// grid, list, map
        /// </summary>
        public string? ViewMode { get; set; } = "grid";

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
    }

    public class RoomCardViewModel
    {
        public int RoomId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public string RoomCode { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;

        public decimal RoomPrice { get; set; }
        public string FormattedPrice { get; set; } = string.Empty;
        public string FormattedPriceCompact { get; set; } = string.Empty;

        public double? Area { get; set; }
        public string FormattedArea { get; set; } = string.Empty;
        public int? Floor { get; set; }
        public int? Capacity { get; set; }

        public RoomStatus Status { get; set; }
        public RoomApprovalStatus ApprovalStatus { get; set; }
        public bool IsFeatured { get; set; }
        public DateTime? PublishedAt { get; set; }
        public DateTime CreatedAt { get; set; }

        public string? ThumbnailUrl { get; set; }
        public int ImageCount { get; set; }

        public int PropertyId { get; set; }
        public string PropertyName { get; set; } = string.Empty;
        public string? FullAddress { get; set; }
        public string ShortAddress { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public int LandlordId { get; set; }
        public string LandlordName { get; set; } = string.Empty;
        public string? LandlordAvatar { get; set; }
        public string? LandlordPhone { get; set; }
        public string LandlordSlug { get; set; } = string.Empty;

        public List<string> AmenityNames { get; set; } = new List<string>();
        public bool IsFavorite { get; set; }

        public double AverageRating { get; set; } = 5.0;
        public int ReviewCount { get; set; } = 0;
    }

    public class RoomSearchResultViewModel
    {
        public List<RoomCardViewModel> Items { get; set; } = new List<RoomCardViewModel>();
        public int TotalItems { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalItems / (double)PageSize) : 0;
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;

        public RoomFilterCriteria Criteria { get; set; } = new RoomFilterCriteria();

        public List<tblProvince> AvailableProvinces { get; set; } = new List<tblProvince>();
        public List<tblDistrict> AvailableDistricts { get; set; } = new List<tblDistrict>();
        public List<tblWard> AvailableWards { get; set; } = new List<tblWard>();
        public List<tblRoomType> AvailableRoomTypes { get; set; } = new List<tblRoomType>();
        public List<tblAmenity> AvailableAmenities { get; set; } = new List<tblAmenity>();

        public string? SelectedProvinceName { get; set; }
        public string? SelectedDistrictName { get; set; }
    }

    public class LandlordPublicProfileViewModel
    {
        public tblLandlord Landlord { get; set; } = null!;
        public int TotalRooms { get; set; }
        public int TotalProperties { get; set; }
        public double AverageRating { get; set; } = 5.0;
        public int TotalReviews { get; set; } = 0;
        public List<RoomCardViewModel> ActiveListings { get; set; } = new List<RoomCardViewModel>();
    }
}
