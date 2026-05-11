using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Areas.Admin.Data;

var builder = WebApplication.CreateBuilder(args);
var connection = builder.Configuration.GetConnectionString("DefaultConnection");

// ── EF Core ──────────────────────────────────────────────────────────
builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlServer(connection));

// ── MVC ──────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();

// ── Session ───────────────────────────────────────────────────────────
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout        = TimeSpan.FromHours(4);
    options.Cookie.HttpOnly    = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name        = ".QuanLyPhongTro.Session";
});

var app = builder.Build();

// ── Pipeline ──────────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();        // ← phải đặt TRƯỚC UseAuthorization
app.UseAuthorization();

// ── Routes ────────────────────────────────────────────────────────────
app.MapControllerRoute(
    name:    "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name:    "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
