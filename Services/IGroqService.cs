namespace QuanLyPhongTro.Services
{
    public interface IGroqService
    {
        Task<string> AskAsync(string systemPrompt, string userMessage);
        bool CheckUserRateLimit(string userIp);
    }
}
