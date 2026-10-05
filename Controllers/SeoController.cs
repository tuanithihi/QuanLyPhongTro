using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Areas.Admin.Data;
using QuanLyPhongTro.Models;
using QuanLyPhongTro.Services;
using System.Text;
using System.Xml.Linq;

namespace QuanLyPhongTro.Controllers
{
    public class SeoController : Controller
    {
        private readonly DataContext _db;

        public SeoController(DataContext db)
        {
            _db = db;
        }

        // ── GET: /robots.txt ──────────────────────────────────────────────
        [HttpGet("/robots.txt")]
        [Produces("text/plain")]
        public IActionResult Robots()
        {
            string baseUrl = $"{Request.Scheme}://{Request.Host}";
            var sb = new StringBuilder();
            sb.AppendLine("User-agent: *");
            sb.AppendLine("Allow: /");
            sb.AppendLine("Disallow: /Admin/");
            sb.AppendLine("Disallow: /api/");
            sb.AppendLine($"Sitemap: {baseUrl}/sitemap.xml");

            return Content(sb.ToString(), "text/plain", Encoding.UTF8);
        }

        // ── GET: /sitemap.xml ─────────────────────────────────────────────
        [HttpGet("/sitemap.xml")]
        [Produces("application/xml")]
        public async Task<IActionResult> Sitemap()
        {
            string baseUrl = $"{Request.Scheme}://{Request.Host}";
            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

            var root = new XElement(ns + "urlset");

            // 1. Các trang tĩnh quan trọng
            AddUrl(root, ns, $"{baseUrl}/", "1.0", "daily", DateTime.UtcNow);
            AddUrl(root, ns, $"{baseUrl}/phong-tro", "0.9", "daily", DateTime.UtcNow);
            AddUrl(root, ns, $"{baseUrl}/phong-da-luu", "0.5", "weekly", DateTime.UtcNow);

            // 2. Các tỉnh/thành có phòng đăng
            var activeProvinces = await _db.Provinces
                .AsNoTracking()
                .Where(p => p.Properties.Any(pr => pr.Rooms.Any(r => r.Status == RoomStatus.Available && r.ApprovalStatus == RoomApprovalStatus.Published && r.IsPublished)))
                .ToListAsync();

            foreach (var prov in activeProvinces)
            {
                string pSlug = SlugHelper.ToSlug(prov.Name);
                AddUrl(root, ns, $"{baseUrl}/phong-tro/{pSlug}", "0.8", "weekly", DateTime.UtcNow);
            }

            // 3. Các quận/huyện có phòng đăng
            var activeDistricts = await _db.Districts
                .AsNoTracking()
                .Include(d => d.Province)
                .Where(d => d.Properties.Any(pr => pr.Rooms.Any(r => r.Status == RoomStatus.Available && r.ApprovalStatus == RoomApprovalStatus.Published && r.IsPublished)))
                .ToListAsync();

            foreach (var dist in activeDistricts)
            {
                if (dist.Province != null)
                {
                    string pSlug = SlugHelper.ToSlug(dist.Province.Name);
                    string dSlug = SlugHelper.ToSlug(dist.Name);
                    AddUrl(root, ns, $"{baseUrl}/phong-tro/{pSlug}/{dSlug}", "0.8", "weekly", DateTime.UtcNow);
                }
            }

            // 4. Phòng Published (BẮT BUỘC chỉ lấy Available & Published & IsPublished)
            var publishedRooms = await _db.Rooms
                .AsNoTracking()
                .Where(r => r.Status == RoomStatus.Available
                         && r.ApprovalStatus == RoomApprovalStatus.Published
                         && r.IsPublished)
                .OrderByDescending(r => r.PublishedAt ?? r.CreatedAt)
                .Select(r => new
                {
                    r.RoomId,
                    r.Slug,
                    LastMod = r.PublishedAt ?? r.CreatedAt
                })
                .ToListAsync();

            foreach (var room in publishedRooms)
            {
                string roomUrl = $"{baseUrl}/phong/{room.Slug}-{room.RoomId}";
                AddUrl(root, ns, roomUrl, "0.8", "daily", room.LastMod);
            }

            // 5. Chủ trọ đã được Approved
            var approvedLandlords = await _db.Landlords
                .AsNoTracking()
                .Where(l => l.Status == LandlordStatus.Approved)
                .ToListAsync();

            foreach (var landlord in approvedLandlords)
            {
                string lSlug = SlugHelper.ToSlug(landlord.FullName);
                AddUrl(root, ns, $"{baseUrl}/chu-tro/{lSlug}", "0.7", "weekly", landlord.CreatedAt);
            }

            var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root);
            return Content(doc.ToString(), "application/xml", Encoding.UTF8);
        }

        private static void AddUrl(XElement root, XNamespace ns, string loc, string priority, string changefreq, DateTime lastmod)
        {
            root.Add(new XElement(ns + "url",
                new XElement(ns + "loc", loc),
                new XElement(ns + "lastmod", lastmod.ToString("yyyy-MM-dd")),
                new XElement(ns + "changefreq", changefreq),
                new XElement(ns + "priority", priority)
            ));
        }
    }
}
