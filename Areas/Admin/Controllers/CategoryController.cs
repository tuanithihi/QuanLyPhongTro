using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Models;
using QuanLyThuVien.Utilities;
using QuanLyThuVien.Attributes;
using PagedList.Core;

namespace QuanLyThuVien.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminOnly]
    public class CategoryController : Controller
    {
        private readonly DataContext _context;
        public CategoryController(DataContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Route("Admin/Category/Index/{page?}")]
        [Route("Admin/Category/")]
        public IActionResult Index(int page = 1)
        {
            if (!Functions._IsAdmin)
                return RedirectToAction("Index", "Login");
            
            int pageSize = 5;
            var categories = _context.Categories.OrderBy(c => c.CategoryID);
            var models = new PagedList.Core.PagedList<tblCategory>(categories, page, pageSize);
            return View(models);
        }
        [HttpGet]
        [Route("Admin/Category/Create")]
        public IActionResult Create()
        {
            if (!Functions._IsAdmin)
                return RedirectToAction("Index", "Login");
            
            return View();
        }
        [HttpPost]
        [Route("Admin/Category/Create")]
        [ValidateAntiForgeryToken]
        public IActionResult Create(tblCategory category)
        {
           
            var name = category.CategoryName?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                ModelState.AddModelError("CategoryName", "Tên danh mục là bắt buộc");
            }

          
            if (!string.IsNullOrWhiteSpace(name))
            {
                var exists = _context.Categories.Any(c => c.CategoryName != null && c.CategoryName.ToLower() == name.ToLower());
                if (exists)
                {
                    ModelState.AddModelError("CategoryName", "Tên danh mục đã tồn tại");
                }
            }

            if (ModelState.IsValid)
            {
                category.CreatedAt = DateTime.Now;

                _context.Categories.Add(category);

                _context.SaveChanges();

                return RedirectToAction("Index");
            }

            return View(category);
        }
        [HttpGet]
        [Route("Admin/Category/Edit/{id}")]
        public IActionResult Edit(int id)
        {
            var category = _context.Categories.Find(id);
            if (category == null) return NotFound();
            return View(category);
        }

        [HttpPost]
        [Route("Admin/Category/Edit/{id}")]
        [ValidateAntiForgeryToken]
        public IActionResult Edit([Bind("CategoryID,CategoryName,Description,IsActive")] tblCategory category)
        {
            var name = category.CategoryName?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                ModelState.AddModelError("CategoryName", "Tên danh mục là bắt buộc");
            }

         
            if (!string.IsNullOrWhiteSpace(name))
            {
                var exists = _context.Categories.Any(c => c.CategoryID != category.CategoryID && c.CategoryName != null && c.CategoryName.ToLower() == name.ToLower());
                if (exists)
                {
                    ModelState.AddModelError("CategoryName", "Tên danh mục đã tồn tại");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Categories.Update(category);
                    _context.SaveChanges();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Categories.Any(e => e.CategoryID == category.CategoryID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Index");
            }

            return View(category);
        }
        [HttpGet]
        [Route("Admin/Category/Delete/{id}")]
        public IActionResult Delete(int id)
        {
            var category = _context.Categories.Find(id);
            if (category == null) return NotFound();
            return View(category);
        }

        [HttpPost, ActionName("Delete")]
        [Route("Admin/Category/Delete/{id}")]
        public IActionResult DeleteConfirmed(int id)
        {
            var category = _context.Categories.Find(id);
            if (category == null) return NotFound();
            _context.Categories.Remove(category);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}
