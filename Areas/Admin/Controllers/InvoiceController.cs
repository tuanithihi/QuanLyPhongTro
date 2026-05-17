using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Areas.Admin.Attributes;
using QuanLyPhongTro.Areas.Admin.Data;
using QuanLyPhongTro.Models;
using MiniSoftware;

namespace QuanLyPhongTro.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminOnly]
    public class InvoiceController : Controller
    {
        private readonly DataContext _context;
        private readonly IWebHostEnvironment _env;

        public InvoiceController(DataContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ── INDEX ────────────────────────────────────────────────────────
        public async Task<IActionResult> Index(string? search, string? status, int? month, int? year, int page = 1)
        {
            const int pageSize = 15;

            var query = _context.Invoices
                .Include(i => i.Room)
                .Include(i => i.Contract).ThenInclude(c => c!.Tenant)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(i =>
                    i.InvoiceCode.Contains(search) ||
                    (i.Room != null && i.Room.RoomCode.Contains(search)) ||
                    (i.Contract != null && i.Contract.Tenant != null &&
                     i.Contract.Tenant.FullName.Contains(search)));
            }

            if (!string.IsNullOrEmpty(status) && int.TryParse(status, out int sv))
                query = query.Where(i => i.Status == (InvoiceStatus)sv);

            if (month.HasValue) query = query.Where(i => i.BillingMonth == month.Value);
            if (year.HasValue)  query = query.Where(i => i.BillingYear  == year.Value);

            int total = await query.CountAsync();
            var list  = await query
                .OrderByDescending(i => i.BillingYear)
                .ThenByDescending(i => i.BillingMonth)
                .ThenByDescending(i => i.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync();

            ViewBag.Page       = page;
            ViewBag.PageSize   = pageSize;
            ViewBag.TotalItems = total;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.Search     = search ?? "";
            ViewBag.Status     = status ?? "";
            ViewBag.Month      = month;
            ViewBag.Year       = year ?? DateTime.Now.Year;

            ViewBag.StatusList = new SelectList(new[]
            {
                new { Value = "0", Text = "Chưa thanh toán" },
                new { Value = "1", Text = "Đã thanh toán" },
                new { Value = "2", Text = "Quá hạn" }
            }, "Value", "Text", status);

            return View(list);
        }

        // ── DETAILS ──────────────────────────────────────────────────────
        public async Task<IActionResult> Details(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Room).ThenInclude(r => r!.RoomType)
                .Include(i => i.Contract).ThenInclude(c => c!.Tenant)
                .Include(i => i.InvoiceDetails).ThenInclude(d => d.Service)
                .FirstOrDefaultAsync(i => i.InvoiceId == id);

            if (invoice == null) return NotFound();
            return View(invoice);
        }

        // ── DUE THIS MONTH ────────────────────────────────────────────────
        public async Task<IActionResult> DueThisMonth()
        {
            var today = DateTime.Today;
            var contracts = await _context.Contracts
                .Include(c => c.Room).ThenInclude(r => r!.RoomType)
                .Include(c => c.Tenant)
                .Include(c => c.Invoices.Where(i => i.BillingMonth == today.Month && i.BillingYear == today.Year))
                .Where(c => c.Status == ContractStatus.Active)
                .OrderBy(c => c.PaymentDayOfMonth)
                .ToListAsync();

            ViewBag.Today     = today;
            ViewBag.NeedCount = contracts.Count(c => !c.Invoices.Any());
            return View(contracts);
        }

        // ── GENERATE GET ─────────────────────────────────────────────────
        public async Task<IActionResult> Generate(int contractId, string? from = null)
        {
            var today    = DateTime.Today;
            var contract = await _context.Contracts
                .Include(c => c.Room).ThenInclude(r => r!.RoomType)
                .Include(c => c.Tenant)
                .FirstOrDefaultAsync(c => c.ContractId == contractId && c.Status == ContractStatus.Active);

            if (contract == null)
            {
                TempData["Error"] = "Hợp đồng không tồn tại hoặc không còn hiệu lực.";
                return RedirectToAction(nameof(DueThisMonth));
            }

            var lastInvoice = await _context.Invoices
                .Where(i => i.ContractId == contractId)
                .OrderByDescending(i => i.BillingYear).ThenByDescending(i => i.BillingMonth)
                .FirstOrDefaultAsync();

            int billingMonth, billingYear;
            double electricStart, waterStart;
            if (lastInvoice != null)
            {
                var next = new DateTime(lastInvoice.BillingYear, lastInvoice.BillingMonth, 1).AddMonths(1);
                billingMonth  = next.Month;
                billingYear   = next.Year;
                electricStart = lastInvoice.ElectricIndexEnd;
                waterStart    = lastInvoice.WaterIndexEnd;
            }
            else
            {
                billingMonth  = today.Month;
                billingYear   = today.Year;
                electricStart = contract.InitialElectricIndex;
                waterStart    = contract.InitialWaterIndex;
            }

            var dueDate  = new DateTime(billingYear, billingMonth,
                Math.Min(contract.PaymentDayOfMonth, DateTime.DaysInMonth(billingYear, billingMonth)));
            var services  = await _context.Services.Where(s => s.IsActive).OrderBy(s => s.ServiceType).ToListAsync();
            var elecSvc   = services.FirstOrDefault(s => s.ServiceType == ServiceType.Electric);
            var waterSvc  = services.FirstOrDefault(s => s.ServiceType == ServiceType.Water);

            ViewBag.Contract       = contract;
            ViewBag.ElectricStart  = electricStart;
            ViewBag.WaterStart     = waterStart;
            ViewBag.Services       = services;
            ViewBag.BillingMonth   = billingMonth;
            ViewBag.BillingYear    = billingYear;
            ViewBag.DueDate        = dueDate;
            ViewBag.InvoiceCode    = await GenerateInvoiceCode(billingMonth, billingYear);
            ViewBag.ElecUnitPrice  = elecSvc?.UnitPrice  ?? 0m;
            ViewBag.WaterUnitPrice = waterSvc?.UnitPrice ?? 0m;
            ViewBag.From           = from;
            return View();
        }

        // ── GENERATE POST ────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(
            int contractId,
            tblInvoice model,
            decimal    elecUnitPrice,
            decimal    waterUnitPrice,
            int[]?     serviceIds,
            double[]?  quantities,
            decimal[]? unitPrices,
            string[]?  descriptions,
            string[]?  miscDescriptions,
            decimal[]? miscAmounts)
        {
            // null-safe arrays
            serviceIds       ??= Array.Empty<int>();
            quantities       ??= Array.Empty<double>();
            unitPrices       ??= Array.Empty<decimal>();
            descriptions     ??= Array.Empty<string>();
            miscDescriptions ??= Array.Empty<string>();
            miscAmounts      ??= Array.Empty<decimal>();

            bool duplicate = await _context.Invoices.AnyAsync(i =>
                i.ContractId   == contractId &&
                i.BillingMonth == model.BillingMonth &&
                i.BillingYear  == model.BillingYear);

            if (duplicate)
            {
                TempData["Error"] = $"Hóa đơn tháng {model.BillingMonth}/{model.BillingYear} của phòng này đã tồn tại.";
                return RedirectToAction(nameof(DueThisMonth));
            }

            var details = new List<tblInvoiceDetail>();

            // Điện — tính trực tiếp từ chỉ số đầu/cuối kỳ
            double elecQty = Math.Max(0, model.ElectricIndexEnd - model.ElectricIndexStart);
            if (elecQty > 0 && elecUnitPrice > 0)
            {
                var elecSvc = await _context.Services
                    .FirstOrDefaultAsync(s => s.IsActive && s.ServiceType == ServiceType.Electric);
                details.Add(new tblInvoiceDetail
                {
                    ServiceId   = elecSvc?.ServiceId,
                    Quantity    = elecQty,
                    UnitPrice   = elecUnitPrice,
                    Amount      = (decimal)elecQty * elecUnitPrice,
                    Description = $"Tiền điện tháng {model.BillingMonth}/{model.BillingYear}"
                });
            }

            // Nước — tính trực tiếp từ chỉ số đầu/cuối kỳ
            double waterQty = Math.Max(0, model.WaterIndexEnd - model.WaterIndexStart);
            if (waterQty > 0 && waterUnitPrice > 0)
            {
                var waterSvc = await _context.Services
                    .FirstOrDefaultAsync(s => s.IsActive && s.ServiceType == ServiceType.Water);
                details.Add(new tblInvoiceDetail
                {
                    ServiceId   = waterSvc?.ServiceId,
                    Quantity    = waterQty,
                    UnitPrice   = waterUnitPrice,
                    Amount      = (decimal)waterQty * waterUnitPrice,
                    Description = $"Tiền nước tháng {model.BillingMonth}/{model.BillingYear}"
                });
            }

            // Dịch vụ cố định khác
            for (int i = 0; i < serviceIds.Length; i++)
            {
                double  qty   = i < quantities.Length  ? quantities[i]  : 0;
                decimal price = i < unitPrices.Length  ? unitPrices[i]  : 0;
                if (qty <= 0 && price <= 0) continue;
                details.Add(new tblInvoiceDetail
                {
                    ServiceId   = serviceIds[i],
                    Quantity    = qty,
                    UnitPrice   = price,
                    Amount      = (decimal)qty * price,
                    Description = descriptions.ElementAtOrDefault(i)
                });
            }

            // Chi phí phát sinh (ServiceId = null)
            for (int i = 0; i < miscDescriptions.Length; i++)
            {
                decimal amt = i < miscAmounts.Length ? miscAmounts[i] : 0;
                if (amt <= 0) continue;
                details.Add(new tblInvoiceDetail
                {
                    ServiceId   = null,
                    Quantity    = 1,
                    UnitPrice   = amt,
                    Amount      = amt,
                    Description = miscDescriptions[i]
                });
            }

            model.ContractId         = contractId;
            model.TotalServiceAmount = details.Sum(d => d.Amount);
            model.TotalAmount        = model.RoomRentAmount + model.TotalServiceAmount - model.Discount;
            model.CreatedAt          = DateTime.Now;
            model.InvoiceDetails     = details;

            _context.Invoices.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã gửi hóa đơn {model.InvoiceCode} cho người thuê thành công.";
            return RedirectToAction(nameof(Details), new { id = model.InvoiceId });
        }

        // ── SELECT ROOM ──────────────────────────────────────────────────
        public async Task<IActionResult> SelectRoom()
        {
            var contracts = await _context.Contracts
                .Include(c => c.Room).ThenInclude(r => r!.RoomType)
                .Include(c => c.Tenant)
                .Include(c => c.Invoices)
                .Where(c => c.Status == ContractStatus.Active)
                .OrderBy(c => c.Room!.RoomCode)
                .ToListAsync();

            return View(contracts);
        }

        // ── CREATE GET — redirect sang Generate (dùng chung form) ──────
        public IActionResult Create(int contractId)
            => RedirectToAction(nameof(Generate), new { contractId, from = "select" });

        // ── CREATE POST ──────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            tblInvoice model,
            int[]    serviceIds,
            double[] quantities,
            decimal[] unitPrices,
            string[] descriptions)
        {
            // Kiểm tra trùng kỳ
            bool duplicate = await _context.Invoices.AnyAsync(i =>
                i.ContractId    == model.ContractId &&
                i.BillingMonth  == model.BillingMonth &&
                i.BillingYear   == model.BillingYear);
            if (duplicate)
                ModelState.AddModelError("", $"Hóa đơn tháng {model.BillingMonth}/{model.BillingYear} đã tồn tại.");

            if (!ModelState.IsValid)
            {
                var contract2 = await _context.Contracts.Include(c => c.Room).Include(c => c.Tenant)
                    .FirstOrDefaultAsync(c => c.ContractId == model.ContractId);
                var services2 = await _context.Services.Where(s => s.IsActive).OrderBy(s => s.ServiceType).ToListAsync();
                ViewBag.Contract = contract2;
                ViewBag.Services = services2;
                return View(model);
            }

            // Tạo InvoiceDetails từ mảng dữ liệu form
            var details = new List<tblInvoiceDetail>();
            for (int i = 0; i < serviceIds.Length; i++)
            {
                if (quantities[i] <= 0 && unitPrices[i] <= 0) continue;
                details.Add(new tblInvoiceDetail
                {
                    ServiceId   = serviceIds[i],
                    Quantity    = quantities[i],
                    UnitPrice   = unitPrices[i],
                    Amount      = (decimal)quantities[i] * unitPrices[i],
                    Description = descriptions.ElementAtOrDefault(i)
                });
            }

            model.TotalServiceAmount = details.Sum(d => d.Amount);
            model.TotalAmount        = model.RoomRentAmount + model.TotalServiceAmount - model.Discount;
            model.CreatedAt          = DateTime.Now;
            model.InvoiceDetails     = details;

            _context.Invoices.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã lập hóa đơn {model.InvoiceCode} thành công.";
            return RedirectToAction(nameof(Details), new { id = model.InvoiceId });
        }

        // ── EDIT GET ─────────────────────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Room)
                .Include(i => i.Contract).ThenInclude(c => c!.Tenant)
                .Include(i => i.InvoiceDetails).ThenInclude(d => d.Service)
                .FirstOrDefaultAsync(i => i.InvoiceId == id);

            if (invoice == null) return NotFound();

            ViewBag.StatusList = BuildStatusList(invoice.Status);
            return View(invoice);
        }

        // ── EDIT POST ────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, tblInvoice model)
        {
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice == null) return NotFound();

            if (model.Status == InvoiceStatus.Paid && model.PaidDate == null)
                ModelState.AddModelError("PaidDate", "Vui lòng nhập ngày thanh toán.");

            if (!ModelState.IsValid)
            {
                ViewBag.StatusList = BuildStatusList(invoice.Status);
                return View(model);
            }

            invoice.Status        = model.Status;
            invoice.PaidDate      = model.Status == InvoiceStatus.Paid ? model.PaidDate : null;
            invoice.PaymentMethod = model.PaymentMethod;
            invoice.Discount      = model.Discount;
            invoice.TotalAmount   = invoice.RoomRentAmount + invoice.TotalServiceAmount - model.Discount;
            invoice.Notes         = model.Notes;
            invoice.DueDate       = model.DueDate;
            invoice.UpdatedAt     = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã cập nhật hóa đơn thành công.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ── MARK PAID (quick) ────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkPaid(int id)
        {
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice == null) return NotFound();

            invoice.Status    = InvoiceStatus.Paid;
            invoice.PaidDate  = DateTime.Today;
            invoice.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã xác nhận thanh toán hóa đơn {invoice.InvoiceCode}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ── DELETE ──────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Contract)
                .FirstOrDefaultAsync(i => i.InvoiceId == id);
            if (invoice == null) return NotFound();

            int contractId = invoice.ContractId;
            // InvoiceDetail có CASCADE DELETE → tự xóa khi xóa Invoice
            _context.Invoices.Remove(invoice);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã xóa hóa đơn {invoice.InvoiceCode}.";
            return RedirectToAction("Details", "Contract", new { id = contractId });
        }

        // ── AJAX: lấy chỉ số kỳ trước ────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetPreviousIndex(int contractId, int month, int year)
        {
            var prev = await _context.Invoices
                .Where(i => i.ContractId == contractId &&
                            (i.BillingYear < year || (i.BillingYear == year && i.BillingMonth < month)))
                .OrderByDescending(i => i.BillingYear).ThenByDescending(i => i.BillingMonth)
                .Select(i => new { i.ElectricIndexEnd, i.WaterIndexEnd })
                .FirstOrDefaultAsync();

            if (prev != null) return Json(new { ok = true,  electricStart = prev.ElectricIndexEnd, waterStart = prev.WaterIndexEnd });

            var contract = await _context.Contracts.FindAsync(contractId);
            return Json(new { ok = true, electricStart = contract?.InitialElectricIndex ?? 0, waterStart = contract?.InitialWaterIndex ?? 0 });
        }

        // ── EXPORT WORD ──────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> ExportWord(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Room)
                .Include(i => i.Contract).ThenInclude(c => c!.Tenant)
                .Include(i => i.InvoiceDetails).ThenInclude(d => d.Service)
                .FirstOrDefaultAsync(i => i.InvoiceId == id);

            if (invoice == null) return NotFound();

            string templatePath = Path.Combine(_env.WebRootPath, "templates", "HoaDon_Template.docx");
            if (!System.IO.File.Exists(templatePath))
            {
                TempData["Error"] = "Không tìm thấy file template hóa đơn (HoaDon_Template.docx) trong thư mục wwwroot/templates.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var rows = invoice.InvoiceDetails.Select(d => new Dictionary<string, object>
            {
                ["ServiceName"] = d.Service?.ServiceName ?? d.Description ?? "Dịch vụ khác",
                ["Qty"] = d.Quantity.ToString("N2"),
                ["Price"] = d.UnitPrice.ToString("N0"),
                ["Amount"] = d.Amount.ToString("N0")
            }).ToList();

            var value = new Dictionary<string, object>
            {
                ["InvoiceCode"] = invoice.InvoiceCode,
                ["BillingPeriod"] = $"{invoice.BillingMonth}/{invoice.BillingYear}",
                ["DueDate"] = invoice.DueDate.ToString("dd/MM/yyyy"),
                
                ["RoomCode"] = invoice.Room?.RoomCode ?? "",
                ["RoomName"] = invoice.Room?.RoomName ?? "",
                ["TenantName"] = invoice.Contract?.Tenant?.FullName ?? "",
                
                ["ElectricStart"] = invoice.ElectricIndexStart.ToString("N1"),
                ["ElectricEnd"] = invoice.ElectricIndexEnd.ToString("N1"),
                ["ElectricQty"] = (invoice.ElectricIndexEnd - invoice.ElectricIndexStart).ToString("N1"),
                
                ["WaterStart"] = invoice.WaterIndexStart.ToString("N1"),
                ["WaterEnd"] = invoice.WaterIndexEnd.ToString("N1"),
                ["WaterQty"] = (invoice.WaterIndexEnd - invoice.WaterIndexStart).ToString("N1"),
                
                ["RoomRent"] = invoice.RoomRentAmount.ToString("N0"),
                ["TotalService"] = invoice.TotalServiceAmount.ToString("N0"),
                ["Discount"] = invoice.Discount.ToString("N0"),
                ["TotalAmount"] = invoice.TotalAmount.ToString("N0"),
                
                ["rows"] = rows
            };

            var memoryStream = new MemoryStream();
            MiniWord.SaveAsByTemplate(memoryStream, templatePath, value);
            memoryStream.Position = 0;

            string fileName = $"HoaDon_{invoice.InvoiceCode}.docx";
            return File(memoryStream, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
        }

        // ── HELPERS ──────────────────────────────────────────────────────
        private async Task<string> GenerateInvoiceCode(int month, int year)
        {
            var prefix = $"HĐ{year}{month:D2}";
            var last = await _context.Invoices
                .Where(i => i.InvoiceCode.StartsWith(prefix))
                .OrderByDescending(i => i.InvoiceCode)
                .Select(i => i.InvoiceCode)
                .FirstOrDefaultAsync();

            int seq = 1;
            if (last != null && last.Length > prefix.Length &&
                int.TryParse(last[prefix.Length..], out int n))
                seq = n + 1;

            return $"{prefix}{seq:D3}";
        }

        private SelectList BuildStatusList(InvoiceStatus current) =>
            new SelectList(new[]
            {
                new { Value = "0", Text = "Chưa thanh toán" },
                new { Value = "1", Text = "Đã thanh toán" },
                new { Value = "2", Text = "Quá hạn" }
            }, "Value", "Text", ((int)current).ToString());
    }
}
