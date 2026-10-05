namespace QuanLyPhongTro.Services
{
    public interface IFileUploadService
    {
        Task<(bool IsValid, string? ErrorMessage, string? RelativePath)> UploadImageAsync(
            IFormFile? file,
            string targetFolder = "images/rooms",
            long maxFileSizeBytes = 5 * 1024 * 1024);

        Task<(bool IsValid, string? ErrorMessage, string? RelativePath, string? ThumbnailRelativePath)> UploadImageWithThumbnailAsync(
            IFormFile? file,
            string targetFolder = "images/rooms",
            int thumbMaxWidth = 400,
            int thumbMaxHeight = 300,
            long maxFileSizeBytes = 5 * 1024 * 1024);

        Task<List<string>> UploadImagesAsync(
            IEnumerable<IFormFile>? files,
            string targetFolder = "images/rooms",
            long maxFileSizeBytes = 5 * 1024 * 1024);

        bool DeleteFile(string? relativePath);
    }
}
