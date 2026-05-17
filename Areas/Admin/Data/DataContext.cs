using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Models;

namespace QuanLyPhongTro.Areas.Admin.Data
{
    /// <summary>
    /// DbContext trung tâm của ứng dụng Quản lý Phòng Trọ.
    /// Đăng ký toàn bộ DbSet tương ứng với các bảng trong CSDL.
    /// </summary>
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }

        // ═══════════════════════════════════════════════════════════════
        //  DbSets  ─  mỗi property = 1 bảng trong SQL Server
        // ═══════════════════════════════════════════════════════════════

        /// <summary>Loại phòng (Studio, 1PN, 2PN...)</summary>
        public DbSet<tblRoomType> RoomTypes { get; set; }

        /// <summary>Phòng trọ</summary>
        public DbSet<tblRoom> Rooms { get; set; }

        /// <summary>Người thuê trọ</summary>
        public DbSet<tblTenant> Tenants { get; set; }

        /// <summary>Hợp đồng thuê phòng</summary>
        public DbSet<tblContract> Contracts { get; set; }

        /// <summary>Bảng giá dịch vụ (điện, nước, rác, wifi...)</summary>
        public DbSet<tblService> Services { get; set; }

        /// <summary>Hóa đơn hàng tháng</summary>
        public DbSet<tblInvoice> Invoices { get; set; }

        /// <summary>Chi tiết các khoản thu trong hóa đơn</summary>
        public DbSet<tblInvoiceDetail> InvoiceDetails { get; set; }

        /// <summary>Bài viết thông báo</summary>
        public DbSet<tblPost> Posts { get; set; }

        /// <summary>Menu điều hướng website</summary>
        public DbSet<tblMenu> Menus { get; set; }

        /// <summary>Người dùng đăng ký trên website</summary>
        public DbSet<tblUser> Users { get; set; }

        /// <summary>Đánh giá từ khách hàng (testimonial trang chủ)</summary>
        public DbSet<tblReview> Reviews { get; set; }

        /// <summary>Đánh giá phòng trọ cụ thể từ khách hàng</summary>
        public DbSet<tblRoomReview> RoomReviews { get; set; }

        /// <summary>Yêu cầu đặt lịch xem phòng và tin nhắn liên hệ</summary>
        public DbSet<tblBookingRequest> BookingRequests { get; set; }

        /// <summary>Phiên chat giữa khách và quản trị viên</summary>
        public DbSet<tblChatSession> ChatSessions { get; set; }

        /// <summary>Tin nhắn trong mỗi phiên chat</summary>
        public DbSet<tblChatMessage> ChatMessages { get; set; }

        // ═══════════════════════════════════════════════════════════════
        //  OnModelCreating  ─  cấu hình quan hệ & ràng buộc
        // ═══════════════════════════════════════════════════════════════

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── tblRoomType ─────────────────────────────────────────────
            modelBuilder.Entity<tblRoomType>(entity =>
            {
                entity.HasIndex(rt => rt.RoomTypeName).IsUnique();
            });

            // ── tblRoom ─────────────────────────────────────────────────
            modelBuilder.Entity<tblRoom>(entity =>
            {
                entity.HasIndex(r => r.RoomCode).IsUnique();

                entity.Property(r => r.RoomPrice).HasColumnType("decimal(18,2)");
                entity.Property(r => r.DefaultDeposit).HasColumnType("decimal(18,2)");

                entity.HasOne(r => r.RoomType)
                      .WithMany(rt => rt.Rooms)
                      .HasForeignKey(r => r.RoomTypeId)
                      .OnDelete(DeleteBehavior.Restrict);  // Không xóa loại phòng khi còn phòng liên kết
            });

            // ── tblTenant ────────────────────────────────────────────────
            modelBuilder.Entity<tblTenant>(entity =>
            {
                entity.HasIndex(t => t.IdentityNumber).IsUnique();
                // Username là nullable nhưng nếu có giá trị thì phải unique
                entity.HasIndex(t => t.Username)
                      .IsUnique()
                      .HasFilter("[Username] IS NOT NULL");
            });

            // ── tblContract ──────────────────────────────────────────────
            modelBuilder.Entity<tblContract>(entity =>
            {
                entity.HasIndex(c => c.ContractCode).IsUnique();

                entity.Property(c => c.MonthlyRent).HasColumnType("decimal(18,2)");
                entity.Property(c => c.Deposit).HasColumnType("decimal(18,2)");

                entity.HasOne(c => c.Room)
                      .WithMany(r => r.Contracts)
                      .HasForeignKey(c => c.RoomId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.Tenant)
                      .WithMany(t => t.Contracts)
                      .HasForeignKey(c => c.TenantId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ── tblService ───────────────────────────────────────────────
            modelBuilder.Entity<tblService>(entity =>
            {
                entity.Property(s => s.UnitPrice).HasColumnType("decimal(18,2)");
            });

            // ── tblInvoice ───────────────────────────────────────────────
            modelBuilder.Entity<tblInvoice>(entity =>
            {
                entity.HasIndex(i => i.InvoiceCode).IsUnique();

                entity.Property(i => i.RoomRentAmount).HasColumnType("decimal(18,2)");
                entity.Property(i => i.TotalServiceAmount).HasColumnType("decimal(18,2)");
                entity.Property(i => i.Discount).HasColumnType("decimal(18,2)");
                entity.Property(i => i.TotalAmount).HasColumnType("decimal(18,2)");

                entity.HasOne(i => i.Room)
                      .WithMany(r => r.Invoices)
                      .HasForeignKey(i => i.RoomId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(i => i.Contract)
                      .WithMany(c => c.Invoices)
                      .HasForeignKey(i => i.ContractId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ── tblInvoiceDetail ─────────────────────────────────────────
            modelBuilder.Entity<tblInvoiceDetail>(entity =>
            {
                entity.Property(d => d.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(d => d.Amount).HasColumnType("decimal(18,2)");

                entity.HasOne(d => d.Invoice)
                      .WithMany(i => i.InvoiceDetails)
                      .HasForeignKey(d => d.InvoiceId)
                      .OnDelete(DeleteBehavior.Cascade);  // Xóa hóa đơn → xóa luôn chi tiết

                entity.HasOne(d => d.Service)
                      .WithMany(s => s.InvoiceDetails)
                      .HasForeignKey(d => d.ServiceId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ── tblPost ──────────────────────────────────────────────────
            modelBuilder.Entity<tblPost>(entity =>
            {
                entity.HasIndex(p => p.Slug).IsUnique();
            });

            // ── tblMenu (self-referencing) ────────────────────────────────
            modelBuilder.Entity<tblMenu>(entity =>
            {
                entity.HasOne(m => m.ParentMenu)
                      .WithMany(m => m.ChildMenus)
                      .HasForeignKey(m => m.ParentMenuId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ── tblUser ──────────────────────────────────────────────────
            modelBuilder.Entity<tblUser>(entity =>
            {
                entity.HasIndex(u => u.Username).IsUnique();
                entity.HasIndex(u => u.Email).IsUnique();
            });

            // ── tblReview ────────────────────────────────────────────────
            modelBuilder.Entity<tblReview>(entity =>
            {
                entity.Property(r => r.Rating).HasDefaultValue(5);
                entity.Property(r => r.IsApproved).HasDefaultValue(true);
            });

            // ── tblRoomReview ─────────────────────────────────────────────
            modelBuilder.Entity<tblRoomReview>(entity =>
            {
                entity.Property(r => r.Rating).HasDefaultValue(5);
                entity.Property(r => r.IsApproved).HasDefaultValue(true);

                entity.HasOne(r => r.Room)
                      .WithMany(room => room.RoomReviews)
                      .HasForeignKey(r => r.RoomId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ── tblBookingRequest ─────────────────────────────────────────
            modelBuilder.Entity<tblBookingRequest>(entity =>
            {
                entity.HasOne(b => b.Room)
                      .WithMany()
                      .HasForeignKey(b => b.RoomId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ── tblChatSession ────────────────────────────────────────────
            modelBuilder.Entity<tblChatSession>(entity =>
            {
                entity.HasIndex(s => s.SessionKey).IsUnique();

                entity.HasOne(s => s.Tenant)
                      .WithMany()
                      .HasForeignKey(s => s.TenantId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(s => s.User)
                      .WithMany()
                      .HasForeignKey(s => s.UserId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // ── tblChatMessage ────────────────────────────────────────────
            modelBuilder.Entity<tblChatMessage>(entity =>
            {
                entity.HasOne(m => m.Session)
                      .WithMany(s => s.Messages)
                      .HasForeignKey(m => m.SessionId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

        }
    }
}
