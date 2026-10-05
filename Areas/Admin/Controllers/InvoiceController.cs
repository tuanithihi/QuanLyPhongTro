using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Areas.Admin.Attributes;
using QuanLyPhongTro.Areas.Admin.Data;
using QuanLyPhongTro.Models;
using QuanLyPhongTro.Services;
using MiniSoftware;

namespace QuanLyPhongTro.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminOnly]
    public class InvoiceController : Controller
    {
        private readonly DataContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ICurrentLandlordService _currentLandlordService;
        private readonly IConfiguration _config;

        public InvoiceController(
            DataContext context,
            IWebHostEnvironment env,
            ICurrentLandlordService currentLandlordService,
            IConfiguration config)
        {
            _context = context;
            _env = env;
            _currentLandlordService = currentLandlordService;
            _config = config;
        }

        // ── INDEX ────────────────────────────────────────────────────────
        public async Task<IActionResult> Index(string? search, string? status, int? month, int? year, int page = 1)
        {
            const int pageSize = 15;

            var query = _context.Invoices
                .Include(i => i.Room)
                .Include(i => i.Contract).ThenInclude(c => c!.Tenant)
                .AsQueryable();

            // Cô lập dữ liệu theo chủ trọ nếu không phải SuperAdmin
            if (!_currentLandlordService.IsSuperAdmin())
            {
                int? currentLandlordId = _currentLandlordService.GetCurrentLandlordId();
                query = query.Where(i => i.LandlordId == currentLandlordId);
            }

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
            ViewBag.Year       = year;

            ViewBag.StatusList = new SelectList(new[]
            {
                new { Value = "0", Text = "Chưa thanh toán" },
                new { Value = "1", Text = "Đã thanh toán" },
                new { Value = "2", Text = "Quá hạn" }
            }, "Value", "Text", status);

            return View(list);
        }

        // ── DETAILS (Chống IDOR) ─────────────────────────────────────────
        public async Task<IActionResult> Details(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Room).ThenInclude(r => r!.RoomType)
                .Include(i => i.Contract).ThenInclude(c => c!.Tenant)
                .Include(i => i.InvoiceDetails).ThenInclude(d => d.Service)
                .FirstOrDefaultAsync(i => i.InvoiceId == id);

            if (invoice == null) return NotFound();

            // Chống IDOR
            if (!_currentLandlordService.IsSuperAdmin())
            {
                int? currentLandlordId = _currentLandlordService.GetCurrentLandlordId();
                if (invoice.LandlordId != currentLandlordId)
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
            }

            return View(invoice);
        }

        // ── DUE THIS MONTH ────────────────────────────────────────────────
        public async Task<IActionResult> DueThisMonth()
        {
            var today = DateTime.Today;
            var query = _context.Contracts
                .Include(c => c.Room).ThenInclude(r => r!.RoomType)
                .Include(c => c.Tenant)
                .Include(c => c.Invoices.Where(i => i.BillingMonth == today.Month && i.BillingYear == today.Year))
                .Where(c => c.Status == ContractStatus.Active)
                .AsQueryable();

            if (!_currentLandlordService.IsSuperAdmin())
            {
                int? currentLandlordId = _currentLandlordService.GetCurrentLandlordId();
                query = query.Where(c => c.LandlordId == currentLandlordId);
            }

            var contracts = await query
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

            // Chống IDOR
            if (!_currentLandlordService.IsSuperAdmin())
            {
                int? currentLandlordId = _currentLandlordService.GetCurrentLandlordId();
                if (contract.LandlordId != currentLandlordId)
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
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
            
            int? targetLandlordId = contract.LandlordId ?? _currentLandlordService.GetCurrentLandlordId();
            var servicesQuery = _context.Services.Where(s => s.IsActive).AsQueryable();
            if (targetLandlordId.HasValue)
            {
                servicesQuery = servicesQuery.Where(s => s.LandlordId == targetLandlordId.Value || s.LandlordId == null);
            }
            else if (!_currentLandlordService.IsSuperAdmin())
            {
                int? currentLandlordId = _currentLandlordService.GetCurrentLandlordId();
                servicesQuery = servicesQuery.Where(s => s.LandlordId == currentLandlordId || s.LandlordId == null);
            }

            var services  = await servicesQuery.OrderBy(s => s.ServiceType).ToListAsync();
            var elecSvc   = services.Where(s => s.ServiceType == ServiceType.Electric).OrderByDescending(s => s.LandlordId.HasValue).FirstOrDefault();
            var waterSvc  = services.Where(s => s.ServiceType == ServiceType.Water).OrderByDescending(s => s.LandlordId.HasValue).FirstOrDefault();

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
            var contract = await _context.Contracts.FindAsync(contractId);
            if (contract == null) return NotFound();

            // Chống IDOR
            if (!_currentLandlordService.IsSuperAdmin())
            {
                int? currentLandlordId = _currentLandlordService.GetCurrentLandlordId();
                if (contract.LandlordId != currentLandlordId)
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
            }

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

            if (model.ElectricIndexEnd < model.ElectricIndexStart)
            {
                TempData["Error"] = $"Lỗi: Chỉ số điện cuối kỳ ({model.ElectricIndexEnd} kWh) không được nhỏ hơn chỉ số đầu kỳ ({model.ElectricIndexStart} kWh).";
                return RedirectToAction(nameof(Generate), new { contractId });
            }

            if (model.WaterIndexEnd < model.WaterIndexStart)
            {
                TempData["Error"] = $"Lỗi: Chỉ số nước cuối kỳ ({model.WaterIndexEnd} m³) không được nhỏ hơn chỉ số đầu kỳ ({model.WaterIndexStart} m³).";
                return RedirectToAction(nameof(Generate), new { contractId });
            }

            var details = new List<tblInvoiceDetail>();

            double elecQty = Math.Max(0, model.ElectricIndexEnd - model.ElectricIndexStart);
            if (elecQty > 0 && elecUnitPrice > 0)
            {
                var elecSvc = await _context.Services
                    .Where(s => s.IsActive && s.ServiceType == ServiceType.Electric && (s.LandlordId == contract.LandlordId || s.LandlordId == null))
                    .OrderByDescending(s => s.LandlordId.HasValue)
                    .FirstOrDefaultAsync();

                details.Add(new tblInvoiceDetail
                {
                    ServiceId   = elecSvc?.ServiceId,
                    Quantity    = elecQty,
                    UnitPrice   = elecUnitPrice,
                    Amount      = (decimal)elecQty * elecUnitPrice,
                    Description = $"Tiền điện tháng {model.BillingMonth}/{model.BillingYear}"
                });
            }

            double waterQty = Math.Max(0, model.WaterIndexEnd - model.WaterIndexStart);
            if (waterQty > 0 && waterUnitPrice > 0)
            {
                var waterSvc = await _context.Services
                    .Where(s => s.IsActive && s.ServiceType == ServiceType.Water && (s.LandlordId == contract.LandlordId || s.LandlordId == null))
                    .OrderByDescending(s => s.LandlordId.HasValue)
                    .FirstOrDefaultAsync();

                details.Add(new tblInvoiceDetail
                {
                    ServiceId   = waterSvc?.ServiceId,
                    Quantity    = waterQty,
                    UnitPrice   = waterUnitPrice,
                    Amount      = (decimal)waterQty * waterUnitPrice,
                    Description = $"Tiền nước tháng {model.BillingMonth}/{model.BillingYear}"
                });
            }

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

            model.LandlordId         = _currentLandlordService.GetCurrentLandlordId() ?? contract.LandlordId;
            model.ContractId         = contractId;
            model.RoomId             = contract.RoomId;
            model.TotalServiceAmount = details.Sum(d => d.Amount);
            model.TotalAmount        = model.RoomRentAmount + model.TotalServiceAmount - model.Discount;
            model.CreatedAt          = DateTime.Now;
            model.InvoiceDetails     = details;

            _context.Invoices.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã gửi hóa đơn {model.InvoiceCode} cho người thuê thành công.";
            return RedirectToAction(nameof(Details), new { id = model.InvoiceId });
        }

        // ── EDIT GET (Chống IDOR) ────────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Room)
                .Include(i => i.Contract).ThenInclude(c => c!.Tenant)
                .Include(i => i.InvoiceDetails).ThenInclude(d => d.Service)
                .FirstOrDefaultAsync(i => i.InvoiceId == id);

            if (invoice == null) return NotFound();

            // Chống IDOR
            if (!_currentLandlordService.IsSuperAdmin())
            {
                int? currentLandlordId = _currentLandlordService.GetCurrentLandlordId();
                if (invoice.LandlordId != currentLandlordId)
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
            }

            ViewBag.StatusList = BuildStatusList(invoice.Status);
            return View(invoice);
        }

        // ── EDIT POST (Chống IDOR) ───────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, tblInvoice model)
        {
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice == null) return NotFound();

            // Chống IDOR
            if (!_currentLandlordService.IsSuperAdmin())
            {
                int? currentLandlordId = _currentLandlordService.GetCurrentLandlordId();
                if (invoice.LandlordId != currentLandlordId)
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
            }

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

        // ── MARK PAID (Chống IDOR) ───────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkPaid(int id)
        {
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice == null) return NotFound();

            // Chống IDOR
            if (!_currentLandlordService.IsSuperAdmin())
            {
                int? currentLandlordId = _currentLandlordService.GetCurrentLandlordId();
                if (invoice.LandlordId != currentLandlordId)
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
            }

            invoice.Status    = InvoiceStatus.Paid;
            invoice.PaidDate  = DateTime.Today;
            invoice.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã xác nhận thanh toán hóa đơn {invoice.InvoiceCode}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ── DELETE (Chống IDOR) ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Contract)
                .FirstOrDefaultAsync(i => i.InvoiceId == id);
            if (invoice == null) return NotFound();

            // Chống IDOR
            if (!_currentLandlordService.IsSuperAdmin())
            {
                int? currentLandlordId = _currentLandlordService.GetCurrentLandlordId();
                if (invoice.LandlordId != currentLandlordId)
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
            }

            int contractId = invoice.ContractId;
            _context.Invoices.Remove(invoice);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã xóa hóa đơn {invoice.InvoiceCode}.";
            return RedirectToAction("Details", "Contract", new { id = contractId });
        }

        // ── EXPORT WORD (Chống IDOR) ─────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> ExportWord(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Room).ThenInclude(r => r!.Property)
                .Include(i => i.Contract).ThenInclude(c => c!.Tenant)
                .Include(i => i.Landlord)
                .Include(i => i.InvoiceDetails).ThenInclude(d => d.Service)
                .FirstOrDefaultAsync(i => i.InvoiceId == id);

            if (invoice == null) return NotFound();

            // Chống IDOR
            if (!_currentLandlordService.IsSuperAdmin())
            {
                int? currentLandlordId = _currentLandlordService.GetCurrentLandlordId();
                if (invoice.LandlordId != currentLandlordId)
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
            }

            string templatePath = Path.Combine(_env.WebRootPath, "templates", "HoaDon_Template.docx");
            if (!System.IO.File.Exists(templatePath))
            {
                TempData["Error"] = "Không tìm thấy file template hóa đơn (HoaDon_Template.docx) trong thư mục wwwroot/templates.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var landlord = invoice.Landlord 
                ?? (invoice.LandlordId.HasValue ? await _context.Landlords.FindAsync(invoice.LandlordId.Value) : null)
                ?? (invoice.Room?.Property?.LandlordId != null ? await _context.Landlords.FindAsync(invoice.Room.Property.LandlordId) : null);

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

                ["LandlordName"] = landlord?.FullName ?? _config["Landlord:Name"] ?? "",
                ["LandlordPhone"] = landlord?.Phone ?? _config["Landlord:Phone"] ?? "",
                ["BankName"] = landlord?.BankName ?? _config["BankPayment:BankName"] ?? "",
                ["BankId"] = landlord?.BankId ?? _config["BankPayment:BankId"] ?? "",
                ["AccountNumber"] = landlord?.AccountNumber ?? _config["BankPayment:AccountNumber"] ?? "",
                ["AccountName"] = landlord?.AccountName ?? _config["BankPayment:AccountName"] ?? "",
                
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

        private SelectList BuildStatusList(InvoiceStatus current)
        {
            return new SelectList(new[]
            {
                new { Value = "0", Text = "Chưa thanh toán" },
                new { Value = "1", Text = "Đã thanh toán" },
                new { Value = "2", Text = "Quá hạn" }
            }, "Value", "Text", ((int)current).ToString());
        }
    }
}
