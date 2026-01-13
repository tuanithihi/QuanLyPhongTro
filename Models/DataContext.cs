using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Areas.Admin.Models;

namespace QuanLyThuVien.Models
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {

        }
        public DbSet<AdminMenu> AdminMenus { get; set; }
        public DbSet<tblUser> Users { get; set; }
        public DbSet<tblCategory> Categories { get; set; }
        public DbSet<tblAuthor> Authors { get; set; }
        // Đã xóa AdminUsers - chỉ dùng tblUser với Role
        // public DbSet<AdminUser> AdminUsers { get; set; }
        public DbSet<tblPublisher> Publishers { get; set; }
        public DbSet<tblBook> Books { get; set; }
        public DbSet<tblBookAuthor> BookAuthors { get; set; }
        public DbSet<tblMenu> Menus { get; set; }
        public DbSet<tblBorrow> Borrows { get; set; }
        public DbSet<tblBorrowDetail> BorrowDetails { get; set; }
        public DbSet<tblStatBookRank> StatBookRanks { get; set; }
        public DbSet<viewPostMenu> viewPostMenus { get; set; }
        public DbSet<viewRecentBook> viewRecentBooks { get; set; }
        public DbSet<tblPost> Posts { get; set; }

    }
}