using elFinder.NetCore;
using elFinder.NetCore.Drivers.FileSystem;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using QuanLyPhongTro.Areas.Admin.Attributes;

namespace QuanLyPhongTro.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminOnly]
    public class FileManagerController : Controller
    {
        private readonly IWebHostEnvironment _env;

        public FileManagerController(IWebHostEnvironment env)
        {
            _env = env;
        }

        public IActionResult Index() => View();

        public async Task<IActionResult> Connector()
        {
            var driver = new FileSystemDriver();
            string root = Path.Combine(_env.WebRootPath, "images");
            Directory.CreateDirectory(root);

            var rootObj = new RootVolume(root, "/images/")
            {
                IsReadOnly    = false,
                IsLocked      = false,
            };

            driver.AddRoot(rootObj);
            var connector = new Connector(driver)
            {
                MimeDetect = MimeDetectOption.Internal
            };
            return await connector.ProcessAsync(Request);
        }
    }
}
