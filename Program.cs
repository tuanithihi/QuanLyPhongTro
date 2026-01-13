using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using QuanLyThuVien.Models;
using QuanLyThuVien.Services;

var builder = WebApplication.CreateBuilder(args);
var connection = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<DataContext>(options => options.UseSqlServer(connection));
// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<ChatbotService>();
builder.Services.AddHttpClient<PdfAnalysisService>();
builder.Services.AddScoped<TextToSpeechService>();




// Cấu hình EmailHelper
QuanLyThuVien.Utilities.EmailHelper.Configure(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions()
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "uploads")),
    RequestPath =  "/files"
});


app.UseRouting();

app.UseAuthorization();
app.MapControllers();
app.MapControllerRoute(
        name: "areas",
     pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
