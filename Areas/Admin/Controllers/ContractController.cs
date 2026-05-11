using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Areas.Admin.Attributes;
using QuanLyPhongTro.Areas.Admin.Data;
using QuanLyPhongTro.Models;

namespace QuanLyPhongTro.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminOnly]
    public class ContractController : Controller
    {
        private readonly DataContext _context;

        public ContractController(DataContext context)
        {
            _context = context;
        }

        // ── INDEX ────────────────────────────────────────────────────────
        public async Task<IActionResult> Index(string? search, string? status, int page = 1)
        {
            const int pageSize = 15;

            var query = _context.Contracts
                .Include(c => c.Room)
                .Include(c => c.Tenant)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(c =>
                    c.ContractCode.Contains(search) ||
                    (c.Room != null && c.Room.RoomCode.Contains(search)) ||
                    (c.Tenant != null && c.Tenant.FullName.Contains(search)));
            }

            if (!string.IsNullOrEmpty(status) && int.TryParse(status, out int statusVal))
            {
                var cs = (ContractStatus)statusVal;
                query = query.Where(c => c.Status == cs);
            }

            int totalItems = await query.CountAsync();
            var list = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Page       = page;
            ViewBag.PageSize   = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.Search     = search ?? "";
            ViewBag.Status     = status ?? "";

            ViewBag.StatusList = new SelectList(new[]
            {
                new { Value = "1", Text = "Đang hiệu lực" },
                new { Value = "0", Text = "Hết hạn" },
                new { Value = "2", Text = "Đã chấm dứt" }
            }, "Value", "Text", status);

            return View(list);
        }

        // ── DETAILS ──────────────────────────────────────────────────────
        public async Task<IActionResult> Details(int id)
        {
            var contract = await _context.Contracts
                .Include(c => c.Room).ThenInclude(r => r!.RoomType)
                .Include(c => c.Tenant)
                .FirstOrDefaultAsync(c => c.ContractId == id);

            if (contract == null) return NotFound();
            return View(contract);
        }

        // ── CREATE GET ───────────────────────────────────────────────────
        public async Task<IActionResult> Create(int? roomId)
        {
            await LoadDropdowns(roomId, null);
            var model = new tblContract
            {
                ContractCode       = await GenerateContractCode(),
                StartDate          = DateTime.Today,
                PaymentDayOfMonth  = 5
            };

            // Nếu chọn phòng sẵn → tự điền giá
            if (roomId.HasValue)
            {
                var room = await _context.Rooms.FindAsync(roomId.Value);
                if (room != null)
                {
                    model.RoomId      = room.RoomId;
                    model.MonthlyRent = room.RoomPrice;
                    model.Deposit     = room.DefaultDeposit;
                }
            }

            return View(model);
        }

        // ── CREATE POST ──────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(tblContract model)
        {
            // Kiểm tra mã hợp đồng trùng
            if (await _context.Contracts.AnyAsync(c => c.ContractCode == model.ContractCode))
                ModelState.AddModelError("ContractCode", "Mã hợp đồng đã tồn tại.");

            // Kiểm tra phòng đã có HĐ active chưa
            if (await _context.Contracts.AnyAsync(c => c.RoomId == model.RoomId && c.Status == ContractStatus.Active))
                ModelState.AddModelError("RoomId", "Phòng này đang có hợp đồng đang hiệu lực.");

            if (!ModelState.IsValid)
            {
                await LoadDropdowns(model.RoomId, model.TenantId);
                return View(model);
            }

            model.CreatedAt = DateTime.Now;
            model.Status    = ContractStatus.Active;

            _context.Contracts.Add(model);

            // Cập nhật trạng thái phòng → Occupied
            var room = await _context.Rooms.FindAsync(model.RoomId);
            if (room != null) room.Status = RoomStatus.Occupied;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã tạo hợp đồng {model.ContractCode} thành công.";
            return RedirectToAction(nameof(Details), new { id = model.ContractId });
        }

        // ── EDIT GET ─────────────────────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            var contract = await _context.Contracts
                .Include(c => c.Room)
                .Include(c => c.Tenant)
                .FirstOrDefaultAsync(c => c.ContractId == id);

            if (contract == null) return NotFound();

            ViewBag.StatusList = BuildStatusSelectList(contract.Status);
            return View(contract);
        }

        // ── EDIT POST ────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, tblContract model)
        {
            if (id != model.ContractId) return NotFound();

            var contract = await _context.Contracts
                .Include(c => c.Room)
                .FirstOrDefaultAsync(c => c.ContractId == id);

            if (contract == null) return NotFound();

            // Validate: nếu Terminated phải có ngày chấm dứt
            if (model.Status == ContractStatus.Terminated && model.ActualEndDate == null)
                ModelState.AddModelError("ActualEndDate", "Vui lòng nhập ngày chấm dứt thực tế.");

            if (!ModelState.IsValid)
            {
                ViewBag.StatusList = BuildStatusSelectList(contract.Status);
                model.Room   = contract.Room;
                model.Tenant = contract.Tenant;
                return View(model);
            }

            bool wasActive   = contract.Status == ContractStatus.Active;
            bool nowActive   = model.Status == ContractStatus.Active;
            bool nowInactive = !nowActive;

            // Cập nhật các field cho phép sửa
            contract.EndDate              = model.EndDate;
            contract.MonthlyRent          = model.MonthlyRent;
            contract.Deposit              = model.Deposit;
            contract.PaymentDayOfMonth    = model.PaymentDayOfMonth;
            contract.InitialElectricIndex = model.InitialElectricIndex;
            contract.InitialWaterIndex    = model.InitialWaterIndex;
            contract.Terms                = model.Terms;
            contract.Notes                = model.Notes;
            contract.Status               = model.Status;
            contract.ActualEndDate        = model.ActualEndDate;
            contract.TerminationReason    = model.TerminationReason;
            contract.UpdatedAt            = DateTime.Now;

            if (contract.Room != null)
            {
                // Hợp đồng kết thúc → phòng về Available
                if (wasActive && nowInactive)
                {
                    var hasOtherActive = await _context.Contracts
                        .AnyAsync(c => c.RoomId == contract.RoomId && c.ContractId != id && c.Status == ContractStatus.Active);
                    if (!hasOtherActive)
                        contract.Room.Status = RoomStatus.Available;
                }
                // Hợp đồng tái kích hoạt → phòng về Occupied
                else if (!wasActive && nowActive)
                {
                    contract.Room.Status = RoomStatus.Occupied;
                }
            }

            await _context.SaveChangesAsync();

            // Khi chấm dứt: chuyển người thuê về danh sách người dùng
            if (wasActive && model.Status == ContractStatus.Terminated)
                await ConvertTenantToUserAsync(contract.TenantId, id);

            // Khi tái kích hoạt: chuyển người dùng về lại người thuê
            if (!wasActive && nowActive)
                await ConvertUserToTenantAsync(contract.TenantId);

            TempData["Success"] = "Đã cập nhật hợp đồng thành công.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ── DELETE POST ──────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var contract = await _context.Contracts
                .Include(c => c.Room)
                .Include(c => c.Invoices)
                .FirstOrDefaultAsync(c => c.ContractId == id);

            if (contract == null) return NotFound();

            if (contract.Invoices.Any())
            {
                TempData["Error"] = "Không thể xóa hợp đồng đã có hóa đơn.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Nếu đang active → trả phòng về Available
            if (contract.Status == ContractStatus.Active && contract.Room != null)
            {
                var hasOtherActive = await _context.Contracts
                    .AnyAsync(c => c.RoomId == contract.RoomId && c.ContractId != id && c.Status == ContractStatus.Active);
                if (!hasOtherActive)
                    contract.Room.Status = RoomStatus.Available;
            }

            _context.Contracts.Remove(contract);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã xóa hợp đồng {contract.ContractCode}.";
            return RedirectToAction(nameof(Index));
        }

        // ── AJAX: lấy giá phòng khi chọn phòng ──────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetRoomPrice(int roomId)
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null) return NotFound();
            return Json(new { price = room.RoomPrice, deposit = room.DefaultDeposit });
        }

        // ── HELPERS ──────────────────────────────────────────────────────
        /// <summary>
        /// Khi hợp đồng bị chấm dứt: tạo tài khoản tblUser từ tenant (nếu có username/email),
        /// xóa thông tin đăng nhập khỏi tenant và đánh dấu không còn active.
        /// Giữ nguyên record tenant để lịch sử hợp đồng không bị gãy FK.
        /// </summary>
        private async Task ConvertTenantToUserAsync(int tenantId, int excludeContractId)
        {
            var tenant = await _context.Tenants.FindAsync(tenantId);

            // Chỉ chuyển nếu tenant có đủ thông tin đăng nhập
            if (tenant == null
                || string.IsNullOrEmpty(tenant.Username)
                || string.IsNullOrEmpty(tenant.PasswordHash)
                || string.IsNullOrEmpty(tenant.Email))
                return;

            // Không chuyển nếu còn hợp đồng active khác
            bool hasOtherActive = await _context.Contracts
                .AnyAsync(c => c.TenantId == tenantId
                            && c.ContractId != excludeContractId
                            && c.Status == ContractStatus.Active);
            if (hasOtherActive) return;

            // Không chuyển nếu username/email đã tồn tại trong tblUser
            bool conflict = await _context.Users
                .AnyAsync(u => u.Username == tenant.Username || u.Email == tenant.Email);
            if (conflict) return;

            // Tạo tài khoản người dùng
            _context.Users.Add(new tblUser
            {
                Username     = tenant.Username,
                Email        = tenant.Email,
                PasswordHash = tenant.PasswordHash,
                FullName     = tenant.FullName,
                Phone        = tenant.Phone,
                Avatar       = tenant.Avatar,
                Role         = "User",
                IsActive     = true,
                CreatedAt    = DateTime.Now
            });

            // Xóa thông tin đăng nhập khỏi tenant, đánh dấu không còn active
            tenant.Username     = null;
            tenant.PasswordHash = null;
            tenant.IsActive     = false;
            tenant.UpdatedAt    = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Khi hợp đồng được tái kích hoạt: tìm tblUser khớp email với tenant,
        /// copy credentials về lại tenant, xóa tblUser, set tenant IsActive = true.
        /// </summary>
        private async Task ConvertUserToTenantAsync(int tenantId)
        {
            var tenant = await _context.Tenants.FindAsync(tenantId);
            if (tenant == null) return;

            // Nếu tenant vẫn còn credentials (chưa từng bị chuyển) → chỉ bật lại active
            if (!string.IsNullOrEmpty(tenant.Username))
            {
                if (!tenant.IsActive) { tenant.IsActive = true; tenant.UpdatedAt = DateTime.Now; await _context.SaveChangesAsync(); }
                return;
            }

            // Tìm tblUser được tạo từ tenant này (khớp email)
            if (!string.IsNullOrEmpty(tenant.Email))
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == tenant.Email && u.Role == "User");

                if (user != null)
                {
                    // Chuyển chat session từ user về tenant
                    await _context.ChatSessions
                        .Where(s => s.UserId == user.UserId)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(x => x.UserId, (int?)null)
                            .SetProperty(x => x.TenantId, tenantId));

                    tenant.Username     = user.Username;
                    tenant.PasswordHash = user.PasswordHash;
                    _context.Users.Remove(user);
                }
            }

            tenant.IsActive  = true;
            tenant.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
        }

        private async Task LoadDropdowns(int? selectedRoomId, int? selectedTenantId)
        {
            // Chỉ hiện phòng Available (hoặc phòng đang chọn)
            var rooms = await _context.Rooms
                .Where(r => r.Status == RoomStatus.Available || r.RoomId == selectedRoomId)
                .OrderBy(r => r.RoomCode)
                .Select(r => new { r.RoomId, Display = $"{r.RoomCode} — {r.RoomName}" })
                .ToListAsync();

            var tenants = await _context.Tenants
                .Where(t => t.IsActive)
                .OrderBy(t => t.FullName)
                .Select(t => new { t.TenantId, Display = $"{t.FullName} ({t.IdentityNumber})" })
                .ToListAsync();

            ViewBag.RoomList   = new SelectList(rooms,   "RoomId",   "Display", selectedRoomId);
            ViewBag.TenantList = new SelectList(tenants, "TenantId", "Display", selectedTenantId);
        }

        private SelectList BuildStatusSelectList(ContractStatus current)
        {
            return new SelectList(new[]
            {
                new { Value = "1", Text = "Đang hiệu lực" },
                new { Value = "0", Text = "Hết hạn" },
                new { Value = "2", Text = "Đã chấm dứt" }
            }, "Value", "Text", ((int)current).ToString());
        }

        private async Task<string> GenerateContractCode()
        {
            var prefix = $"HD{DateTime.Now:yyyyMM}";
            var last = await _context.Contracts
                .Where(c => c.ContractCode.StartsWith(prefix))
                .OrderByDescending(c => c.ContractCode)
                .Select(c => c.ContractCode)
                .FirstOrDefaultAsync();

            int seq = 1;
            if (last != null && last.Length > prefix.Length &&
                int.TryParse(last[prefix.Length..], out int n))
                seq = n + 1;

            return $"{prefix}{seq:D3}";
        }
    }
}
