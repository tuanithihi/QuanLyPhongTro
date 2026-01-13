using System.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PagedList.Core;
using QuanLyThuVien.Models;
using QuanLyThuVien.Utilities;
using static Azure.Core.HttpHeader;

namespace QuanLyThuVien.Areas.Admin.Controllers
{
    [Area("Admin")]
    [QuanLyThuVien.Attributes.AdminOnly]
    public class PostController : Controller
    {
        private readonly DataContext _context;
        public PostController(DataContext context)
        {
            _context = context;
        }

        [Route("Admin/Post/Index/{page?}")]

        public IActionResult Index(int page = 1)
        {
            int pageSize = 5;
            var post = _context.Posts.OrderByDescending(p => p.PostID);
            var models = new PagedList<tblPost>(post, page, pageSize);

            return View(models);
        }

        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Create(tblPost post)
        {
            if (ModelState.IsValid)
            {
                post.MenuID = 1;
                post.CreatedDate = DateTime.Now;
                // set author automatically from current session (admin)
                post.Author = !string.IsNullOrEmpty(Functions._FullName) ? Functions._FullName : "Admin";
                if (post.IsActive == null) post.IsActive = true;

                _context.Posts.Add(post);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        [Route("Admin/Post/Edit/{id}")]
        public IActionResult Edit(int id)
        {
            var post = _context.Posts.Find(id);
            if (post == null) return NotFound();
            return View(post);
        }

        [HttpPost]
        [Route("Admin/Post/Edit/{id}")]
        [ValidateAntiForgeryToken]
        public IActionResult Edit([Bind("PostID,Title,Abstract,Contents,Images,Author,Link,IsActive,PostOrder,MenuID,CreatedDate")] tblPost post)
        {
            if (!ModelState.IsValid)
            {
                return View(post);
            }

            var existing = _context.Posts.Find(post.PostID);
            if (existing == null) return NotFound();

            existing.Title = post.Title;
            existing.Abstract = post.Abstract;
            existing.Contents = post.Contents;
            existing.Images = post.Images;
            existing.Author = post.Author;
            existing.Link = post.Link;
            existing.IsActive = post.IsActive;
            existing.PostOrder = post.PostOrder;
            existing.MenuID = post.MenuID;
            // keep CreatedDate as-is unless provided

            _context.Posts.Update(existing);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        [HttpGet]
        [Route("Admin/Post/Delete/{id}")]
        public IActionResult Delete(int id)
        {
            var post = _context.Posts.Find(id);
            if (post == null) return NotFound();
            return View(post);
        }

        [HttpPost, ActionName("Delete")]
        [Route("Admin/Post/Delete/{id}")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var post = _context.Posts.Find(id);
            if (post == null) return NotFound();
            _context.Posts.Remove(post);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}