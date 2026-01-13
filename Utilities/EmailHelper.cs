using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace QuanLyThuVien.Utilities
{
    public class EmailHelper
    {
        private static IConfiguration _config;

        public static void Configure(IConfiguration config)
        {
            _config = config;
        }

        public static async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            if (_config == null)
                throw new System.Exception("EmailHelper not configured. Call EmailHelper.Configure in Program.cs");

            var sendGridSection = _config.GetSection("SendGrid");
            var apiKey = sendGridSection["ApiKey"];
            var senderEmail = sendGridSection["SenderEmail"];
            var senderName = sendGridSection["SenderName"];

            var client = new SendGridClient(apiKey);
            var from = new EmailAddress(senderEmail, senderName);
            var to = new EmailAddress(toEmail);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, null, htmlMessage);
            var response = await client.SendEmailAsync(msg);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Body.ReadAsStringAsync();
                throw new System.Exception($"SendGrid error: {response.StatusCode} - {body}");
            }
        }
    }
}
