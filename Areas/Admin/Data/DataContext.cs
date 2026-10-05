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

        /// <summary>Chủ trọ</summary>
        public DbSet<tblLandlord> Landlords { get; set; }

        /// <summary>Khu trọ / Tòa nhà trọ</summary>
        public DbSet<tblProperty> Properties { get; set; }

        /// <summary>Tỉnh / Thành phố</summary>
        public DbSet<tblProvince> Provinces { get; set; }

        /// <summary>Quận / Huyện</summary>
        public DbSet<tblDistrict> Districts { get; set; }

        /// <summary>Phường / Xã</summary>
        public DbSet<tblWard> Wards { get; set; }

        /// <summary>Tiện ích phòng</summary>
        public DbSet<tblAmenity> Amenities { get; set; }

        /// <summary>Liên kết phòng - tiện ích</summary>
        public DbSet<tblRoomAmenity> RoomAmenities { get; set; }

        /// <summary>Hình ảnh phòng trọ</summary>
        public DbSet<tblRoomImage> RoomImages { get; set; }

        /// <summary>Danh sách phòng yêu thích</summary>
        public DbSet<tblFavorite> Favorites { get; set; }

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

        /// <summary>Đánh giá từ khách hàng (testimonial trang chủ / hợp đồng)</summary>
        public DbSet<tblReview> Reviews { get; set; }

        /// <summary>Đánh giá phòng trọ cụ thể từ khách hàng</summary>
        public DbSet<tblRoomReview> RoomReviews { get; set; }

        /// <summary>Yêu cầu đặt lịch xem phòng, đặt cọc và tin nhắn liên hệ</summary>
        public DbSet<tblBookingRequest> BookingRequests { get; set; }

        /// <summary>Phiên chat giữa khách và quản trị viên / chủ trọ</summary>
        public DbSet<tblChatSession> ChatSessions { get; set; }

        /// <summary>Tin nhắn trong mỗi phiên chat</summary>
        public DbSet<tblChatMessage> ChatMessages { get; set; }

        // ═══════════════════════════════════════════════════════════════
        //  OnModelCreating  ─  cấu hình quan hệ & ràng buộc
        // ═══════════════════════════════════════════════════════════════

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                foreach (var entityType in modelBuilder.Model.GetEntityTypes())
                {
                    var properties = entityType.ClrType.GetProperties()
                        .Where(p => p.PropertyType == typeof(decimal) || p.PropertyType == typeof(decimal?));
                    foreach (var property in properties)
                    {
                        modelBuilder.Entity(entityType.Name).Property(property.Name).HasConversion<double>();
                    }
                }
            }

            // ── tblLandlord ─────────────────────────────────────────────
            modelBuilder.Entity<tblLandlord>(entity =>
            {
                entity.HasIndex(l => l.Status);
                entity.HasOne(l => l.User)
                      .WithMany()
                      .HasForeignKey(l => l.UserId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // ── tblProvince ─────────────────────────────────────────────
            modelBuilder.Entity<tblProvince>(entity =>
            {
                entity.HasIndex(p => p.Code).IsUnique();
            });

            // ── tblDistrict ─────────────────────────────────────────────
            modelBuilder.Entity<tblDistrict>(entity =>
            {
                entity.HasIndex(d => d.ProvinceId);
                entity.HasIndex(d => d.Code);

                entity.HasOne(d => d.Province)
                      .WithMany(p => p.Districts)
                      .HasForeignKey(d => d.ProvinceId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ── tblWard ─────────────────────────────────────────────────
            modelBuilder.Entity<tblWard>(entity =>
            {
                entity.HasIndex(w => w.DistrictId);
                entity.HasIndex(w => w.Code);

                entity.HasOne(w => w.District)
                      .WithMany(d => d.Wards)
                      .HasForeignKey(w => w.DistrictId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ── tblProperty ─────────────────────────────────────────────
            modelBuilder.Entity<tblProperty>(entity =>
            {
                entity.HasIndex(p => p.Slug).IsUnique();
                entity.HasIndex(p => p.ProvinceId);
                entity.HasIndex(p => p.DistrictId);
                entity.HasIndex(p => p.LandlordId);

                entity.HasOne(p => p.Landlord)
                      .WithMany(l => l.Properties)
                      .HasForeignKey(p => p.LandlordId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Province)
                      .WithMany(pr => pr.Properties)
                      .HasForeignKey(p => p.ProvinceId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.District)
                      .WithMany(d => d.Properties)
                      .HasForeignKey(p => p.DistrictId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Ward)
                      .WithMany(w => w.Properties)
                      .HasForeignKey(p => p.WardId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ── tblAmenity ──────────────────────────────────────────────
            modelBuilder.Entity<tblAmenity>(entity =>
            {
                entity.HasIndex(a => a.Name).IsUnique();
            });

            // ── tblRoomAmenity (Composite Key) ──────────────────────────
            modelBuilder.Entity<tblRoomAmenity>(entity =>
            {
                entity.HasKey(ra => new { ra.RoomId, ra.AmenityId });

                entity.HasOne(ra => ra.Room)
                      .WithMany(r => r.RoomAmenities)
                      .HasForeignKey(ra => ra.RoomId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ra => ra.Amenity)
                      .WithMany(a => a.RoomAmenities)
                      .HasForeignKey(ra => ra.AmenityId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ── tblRoomImage ────────────────────────────────────────────
            modelBuilder.Entity<tblRoomImage>(entity =>
            {
                entity.HasOne(ri => ri.Room)
                      .WithMany(r => r.RoomImages)
                      .HasForeignKey(ri => ri.RoomId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ── tblFavorite ─────────────────────────────────────────────
            modelBuilder.Entity<tblFavorite>(entity =>
            {
                entity.HasIndex(f => f.RoomId);
                entity.HasIndex(f => f.UserId);
                entity.HasIndex(f => f.TenantId);

                entity.HasOne(f => f.Room)
                      .WithMany(r => r.Favorites)
                      .HasForeignKey(f => f.RoomId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(f => f.User)
                      .WithMany()
                      .HasForeignKey(f => f.UserId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(f => f.Tenant)
                      .WithMany(t => t.Favorites)
                      .HasForeignKey(f => f.TenantId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // ── tblRoomType ─────────────────────────────────────────────
            modelBuilder.Entity<tblRoomType>(entity =>
            {
                entity.HasIndex(rt => rt.RoomTypeName).IsUnique();
            });

            // ── tblRoom ─────────────────────────────────────────────────
            modelBuilder.Entity<tblRoom>(entity =>
            {
                entity.HasIndex(r => r.RoomCode).IsUnique();
                entity.HasIndex(r => r.Slug).IsUnique();
                entity.HasIndex(r => r.RoomPrice);          // Index cho Price
                entity.HasIndex(r => r.ApprovalStatus);     // Index cho ApprovalStatus
                entity.HasIndex(r => r.PropertyId);
                entity.HasIndex(r => new { r.IsPublished, r.ApprovalStatus, r.Status });
                entity.HasIndex(r => r.IsFeatured);
                entity.HasIndex(r => r.CreatedAt);

                entity.Property(r => r.RoomPrice).HasColumnType("decimal(18,2)");
                entity.Property(r => r.DefaultDeposit).HasColumnType("decimal(18,2)");

                entity.HasOne(r => r.RoomType)
                      .WithMany(rt => rt.Rooms)
                      .HasForeignKey(r => r.RoomTypeId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Property)
                      .WithMany(p => p.Rooms)
                      .HasForeignKey(r => r.PropertyId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ── tblTenant ────────────────────────────────────────────────
            modelBuilder.Entity<tblTenant>(entity =>
            {
                entity.HasIndex(t => t.IdentityNumber).IsUnique();
                entity.HasIndex(t => t.LandlordId);

                // Username là nullable nhưng nếu có giá trị thì phải unique
                entity.HasIndex(t => t.Username)
                      .IsUnique()
                      .HasFilter("[Username] IS NOT NULL");

                entity.HasOne(t => t.Landlord)
                      .WithMany(l => l.Tenants)
                      .HasForeignKey(t => t.LandlordId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ── tblContract ──────────────────────────────────────────────
            modelBuilder.Entity<tblContract>(entity =>
            {
                entity.HasIndex(c => c.ContractCode).IsUnique();
                entity.HasIndex(c => c.LandlordId);

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

                entity.HasOne(c => c.Landlord)
                      .WithMany(l => l.Contracts)
                      .HasForeignKey(c => c.LandlordId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ── tblService ───────────────────────────────────────────────
            modelBuilder.Entity<tblService>(entity =>
            {
                entity.Property(s => s.UnitPrice).HasColumnType("decimal(18,2)");
                entity.HasIndex(s => s.LandlordId);

                entity.HasOne(s => s.Landlord)
                      .WithMany(l => l.Services)
                      .HasForeignKey(s => s.LandlordId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ── tblInvoice ───────────────────────────────────────────────
            modelBuilder.Entity<tblInvoice>(entity =>
            {
                entity.HasIndex(i => i.InvoiceCode).IsUnique();
                entity.HasIndex(i => i.LandlordId);

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

                entity.HasOne(i => i.Landlord)
                      .WithMany(l => l.Invoices)
                      .HasForeignKey(i => i.LandlordId)
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
                      .OnDelete(DeleteBehavior.Cascade);

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
                entity.HasIndex(r => r.ContractId);

                entity.HasOne(r => r.Contract)
                      .WithMany(c => c.Reviews)
                      .HasForeignKey(r => r.ContractId)
                      .OnDelete(DeleteBehavior.SetNull);
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
                entity.HasIndex(b => b.LandlordId);

                entity.HasOne(b => b.Room)
                      .WithMany()
                      .HasForeignKey(b => b.RoomId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(b => b.Landlord)
                      .WithMany(l => l.BookingRequests)
                      .HasForeignKey(b => b.LandlordId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ── tblChatSession ────────────────────────────────────────────
            modelBuilder.Entity<tblChatSession>(entity =>
            {
                entity.HasIndex(s => s.SessionKey).IsUnique();
                entity.HasIndex(s => s.LandlordId);

                entity.HasOne(s => s.Tenant)
                      .WithMany()
                      .HasForeignKey(s => s.TenantId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(s => s.User)
                      .WithMany()
                      .HasForeignKey(s => s.UserId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(s => s.Landlord)
                      .WithMany(l => l.ChatSessions)
                      .HasForeignKey(s => s.LandlordId)
                      .OnDelete(DeleteBehavior.Restrict);
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
