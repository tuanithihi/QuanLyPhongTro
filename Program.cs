using Microsoft.EntityFrameworkCore;
using QuanLyPhongTro.Areas.Admin.Data;
using QuanLyPhongTro.Services;

var builder = WebApplication.CreateBuilder(args);
var connection = builder.Configuration.GetConnectionString("DefaultConnection");

// ── EF Core ──────────────────────────────────────────────────────────
builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlServer(connection));

// ── MVC ──────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

// ── Groq AI Chatbot ─────────────────────────────────────────────────
builder.Services.AddMemoryCache(); // Cache response AI
builder.Services.AddHttpClient<GroqService>();
builder.Services.AddScoped<IGroqService>(sp => sp.GetRequiredService<GroqService>());

// ── Session ───────────────────────────────────────────────────────────
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout        = TimeSpan.FromHours(4);
    options.Cookie.HttpOnly    = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name        = ".QuanLyPhongTro.Session";
});

// ── Multi-Landlord & Security Services ─────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentLandlordService, CurrentLandlordService>();
builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddScoped<ILandlordService, LandlordService>();
builder.Services.AddScoped<IPropertyService, PropertyService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IRoomSearchService, RoomSearchService>();

var app = builder.Build();

// ── Tự động Seed & Backfill Idempotent ────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<DataContext>();
        var config = services.GetRequiredService<IConfiguration>();
        if (context.Database.CanConnect())
        {
            await DataSeeder.SeedAdministrativeDataAsync(context);
            await DataSeeder.SeedAmenitiesAsync(context);
            await DataSeeder.BackfillLegacyDataAsync(context, config);
            await DataSeeder.SeedDemoDataAsync(context);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Đã xảy ra lỗi trong quá trình tự động Seed và Backfill dữ liệu.");
    }
}

// ── Pipeline ──────────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// ── Security Headers ──────────────────────────────────────────────────
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    await next();
});

app.UseSession();        // ← phải đặt TRƯỚC UseAuthorization
app.UseAuthorization();

// ── Friendly Routes Phía Khách ─────────────────────────────────────────
app.MapControllerRoute(
    name: "room_detail",
    pattern: "phong/{slug}-{id:int}",
    defaults: new { controller = "Home", action = "RoomDetail" });

app.MapControllerRoute(
    name: "listing_district",
    pattern: "phong-tro/{tinh}/{quan}",
    defaults: new { controller = "Home", action = "Listings" });

app.MapControllerRoute(
    name: "listing_province",
    pattern: "phong-tro/{tinh}",
    defaults: new { controller = "Home", action = "Listings" });

app.MapControllerRoute(
    name: "listing_all",
    pattern: "phong-tro",
    defaults: new { controller = "Home", action = "Listings" });

app.MapControllerRoute(
    name: "landlord_profile",
    pattern: "chu-tro/{slug}",
    defaults: new { controller = "Home", action = "LandlordProfile" });

// ── Standard Routes ───────────────────────────────────────────────────
app.MapControllerRoute(
    name:    "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name:    "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

public partial class Program { }
