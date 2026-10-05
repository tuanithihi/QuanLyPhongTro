using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace QuanLyPhongTro.Services
{
    public class FileUploadService : IFileUploadService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<FileUploadService> _logger;

        private static readonly Dictionary<string, (string Mime, byte[][] Signatures)> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            { ".jpg", ("image/jpeg", new[] { new byte[] { 0xFF, 0xD8, 0xFF } }) },
            { ".jpeg", ("image/jpeg", new[] { new byte[] { 0xFF, 0xD8, 0xFF } }) },
            { ".png", ("image/png", new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } }) },
            { ".gif", ("image/gif", new[] { new byte[] { 0x47, 0x49, 0x46, 0x38 } }) },
            { ".webp", ("image/webp", new[] { new byte[] { 0x52, 0x49, 0x46, 0x46 } }) }
        };

        public FileUploadService(IWebHostEnvironment env, ILogger<FileUploadService> logger)
        {
            _env = env;
            _logger = logger;
        }

        public async Task<(bool IsValid, string? ErrorMessage, string? RelativePath)> UploadImageAsync(
            IFormFile? file,
            string targetFolder = "images/rooms",
            long maxFileSizeBytes = 5 * 1024 * 1024)
        {
            var res = await UploadImageWithThumbnailAsync(file, targetFolder, 0, 0, maxFileSizeBytes);
            return (res.IsValid, res.ErrorMessage, res.RelativePath);
        }

        public async Task<(bool IsValid, string? ErrorMessage, string? RelativePath, string? ThumbnailRelativePath)> UploadImageWithThumbnailAsync(
            IFormFile? file,
            string targetFolder = "images/rooms",
            int thumbMaxWidth = 400,
            int thumbMaxHeight = 300,
            long maxFileSizeBytes = 5 * 1024 * 1024)
        {
            if (file == null || file.Length == 0)
            {
                return (false, "File tải lên không có dữ liệu.", null, null);
            }

            // 1. Kiểm tra kích thước file
            if (file.Length > maxFileSizeBytes)
            {
                return (false, $"Dung lượng file vượt quá giới hạn cho phép ({maxFileSizeBytes / (1024 * 1024)}MB).", null, null);
            }

            // 2. Kiểm tra phần mở rộng
            string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !AllowedExtensions.ContainsKey(ext))
            {
                return (false, "Định dạng file không được hỗ trợ. Chỉ chấp nhận các định dạng: .jpg, .jpeg, .png, .webp, .gif.", null, null);
            }

            // 3. Kiểm tra MIME type
            var expectedInfo = AllowedExtensions[ext];
            if (!string.Equals(file.ContentType, expectedInfo.Mime, StringComparison.OrdinalIgnoreCase))
            {
                return (false, "MIME type của file không khớp với định dạng ảnh.", null, null);
            }

            // 4. Kiểm tra Header Signature (Magic Bytes)
            try
            {
                using var stream = file.OpenReadStream();
                byte[] headerBytes = new byte[16];
                int bytesRead = await stream.ReadAsync(headerBytes.AsMemory(0, 16));

                bool matchesSignature = false;
                foreach (var sig in expectedInfo.Signatures)
                {
                    if (bytesRead >= sig.Length && headerBytes.Take(sig.Length).SequenceEqual(sig))
                    {
                        matchesSignature = true;
                        break;
                    }
                }

                if (!matchesSignature)
                {
                    return (false, "Chữ ký nội dung file không hợp lệ hoặc file bị hỏng.", null, null);
                }

                // 5. Đổi tên file ngẫu nhiên và lưu file gốc
                string guidPrefix = Guid.NewGuid().ToString("N");
                string uniqueFileName = string.Concat(guidPrefix, ext);
                string uploadPath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), targetFolder);

                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                string fullPath = Path.Combine(uploadPath, uniqueFileName);
                stream.Position = 0; // Reset stream để copy
                using (var fileStream = new FileStream(fullPath, FileMode.Create))
                {
                    await stream.CopyToAsync(fileStream);
                }

                string relativeUrl = "/" + targetFolder.Trim('/') + "/" + uniqueFileName;
                string? thumbRelativeUrl = null;

                // 6. Tạo thumbnail nếu được yêu cầu (thumbMaxWidth > 0 và thumbMaxHeight > 0)
                if (thumbMaxWidth > 0 && thumbMaxHeight > 0)
                {
                    try
                    {
                        string thumbFileName = string.Concat(guidPrefix, "_thumb", ext);
                        string thumbFullPath = Path.Combine(uploadPath, thumbFileName);

                        stream.Position = 0;
                        using var image = await Image.LoadAsync(stream);
                        image.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Mode = ResizeMode.Max,
                            Size = new Size(thumbMaxWidth, thumbMaxHeight)
                        }));

                        await image.SaveAsync(thumbFullPath);
                        thumbRelativeUrl = "/" + targetFolder.Trim('/') + "/" + thumbFileName;
                    }
                    catch (Exception thumbEx)
                    {
                        _logger.LogWarning(thumbEx, "Không thể tạo thumbnail cho file: {FileName}. Sử dụng ảnh gốc làm fallback.", uniqueFileName);
                        thumbRelativeUrl = relativeUrl;
                    }
                }

                return (true, null, relativeUrl, thumbRelativeUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong quá trình lưu file upload.");
                return (false, "Đã xảy ra lỗi khi lưu file.", null, null);
            }
        }

        public async Task<List<string>> UploadImagesAsync(
            IEnumerable<IFormFile>? files,
            string targetFolder = "images/rooms",
            long maxFileSizeBytes = 5 * 1024 * 1024)
        {
            var result = new List<string>();
            if (files == null) return result;

            foreach (var file in files)
            {
                var (isValid, _, relativePath) = await UploadImageAsync(file, targetFolder, maxFileSizeBytes);
                if (isValid && !string.IsNullOrEmpty(relativePath))
                {
                    result.Add(relativePath);
                }
            }

            return result;
        }

        public bool DeleteFile(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return false;

            try
            {
                string cleanPath = relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                string fullPath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), cleanPath);

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);

                    // Xóa cả file thumbnail đi kèm nếu có
                    string ext = Path.GetExtension(fullPath);
                    string withoutExt = fullPath[..^ext.Length];
                    if (!withoutExt.EndsWith("_thumb"))
                    {
                        string possibleThumb = withoutExt + "_thumb" + ext;
                        if (File.Exists(possibleThumb))
                        {
                            File.Delete(possibleThumb);
                        }
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể xóa file: {Path}", relativePath);
            }

            return false;
        }
    }
}
