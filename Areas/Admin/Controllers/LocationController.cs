using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Areas.Admin.Attributes;
using QuanLyPhongTro.Areas.Admin.Data;
using QuanLyPhongTro.Models;

namespace QuanLyPhongTro.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminOnly]
    [SuperAdminOnly]
    public class LocationController : Controller
    {
        private readonly DataContext _context;

        public LocationController(DataContext context)
        {
            _context = context;
        }

        // GET: /Admin/Location
        public async Task<IActionResult> Index(string? search, int? provinceId, int? districtId)
        {
            var provincesQuery = _context.Provinces.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                provincesQuery = provincesQuery.Where(p => p.Name.Contains(s) || p.Code.Contains(s));
            }

            var provinces = await provincesQuery
                .OrderBy(p => p.Name)
                .Select(p => new ProvinceListItemViewModel
                {
                    ProvinceId = p.ProvinceId,
                    Code = p.Code,
                    Name = p.Name,
                    DistrictCount = p.Districts.Count,
                    PropertyCount = p.Properties.Count,
                    RoomCount = p.Properties.SelectMany(prop => prop.Rooms).Count()
                })
                .ToListAsync();

            ViewBag.Search = search ?? "";
            ViewBag.SelectedProvinceId = provinceId;
            ViewBag.SelectedDistrictId = districtId;

            if (provinceId.HasValue)
            {
                var dists = await _context.Districts
                    .AsNoTracking()
                    .Where(d => d.ProvinceId == provinceId.Value)
                    .OrderBy(d => d.Name)
                    .Select(d => new DistrictListItemViewModel
                    {
                        DistrictId = d.DistrictId,
                        Code = d.Code,
                        Name = d.Name,
                        ProvinceName = d.Province.Name,
                        WardCount = d.Wards.Count,
                        PropertyCount = d.Properties.Count
                    })
                    .ToListAsync();
                ViewBag.Districts = dists;
            }

            if (districtId.HasValue)
            {
                var wards = await _context.Wards
                    .AsNoTracking()
                    .Where(w => w.DistrictId == districtId.Value)
                    .OrderBy(w => w.Name)
                    .ToListAsync();
                ViewBag.Wards = wards;
            }

            return View(provinces);
        }
    }

    public class ProvinceListItemViewModel
    {
        public int ProvinceId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int DistrictCount { get; set; }
        public int PropertyCount { get; set; }
        public int RoomCount { get; set; }
    }

    public class DistrictListItemViewModel
    {
        public int DistrictId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ProvinceName { get; set; } = string.Empty;
        public int WardCount { get; set; }
        public int PropertyCount { get; set; }
    }
}
