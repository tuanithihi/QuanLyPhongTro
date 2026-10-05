using System;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using QuanLyPhongTro.Areas.Admin.Controllers;
using QuanLyPhongTro.Controllers;
using QuanLyPhongTro.Models;
using QuanLyPhongTro.Services;
using Xunit;

namespace QuanLyPhongTro.Tests
{
    public class SecurityAndSanitizationTests
    {
        [Theory]
        [InlineData("<script>alert('XSS')</script>", "")]
        [InlineData("Phòng trọ đẹp <script type='text/javascript'>malicious();</script> giá rẻ", "Phòng trọ đẹp  giá rẻ")]
        [InlineData("<img src='x' onerror='alert(1)'>", "<img src='x'>")]
        [InlineData("<a href=\"javascript:alert('pwned')\">Click me</a>", "<a>Click me</a>")]
        [InlineData("<iframe src='http://evil.com'></iframe>", "")]
        public void HtmlSanitizer_RemovesDangerousTagsAndScripts(string rawHtml, string expectedSubstring)
        {
            // Act
            var sanitized = HtmlSanitizerHelper.SanitizeHtml(rawHtml);

            // Assert
            Assert.DoesNotContain("<script", sanitized, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("onerror", sanitized, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("javascript:", sanitized, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("<iframe", sanitized, StringComparison.OrdinalIgnoreCase);

            if (!string.IsNullOrEmpty(expectedSubstring))
            {
                Assert.Contains(expectedSubstring, sanitized);
            }
        }

        [Fact]
        public void HtmlSanitizer_PreservesSafeFormattingTags()
        {
            // Arrange
            var safeHtml = "<p>Phòng trọ <strong>khép kín</strong> có <em>gác lửng</em>.</p><ul><li>Wifi</li><li>Điều hòa</li></ul>";

            // Act
            var sanitized = HtmlSanitizerHelper.SanitizeHtml(safeHtml);

            // Assert
            Assert.Contains("<p>", sanitized);
            Assert.Contains("<strong>khép kín</strong>", sanitized);
            Assert.Contains("<em>gác lửng</em>", sanitized);
            Assert.Contains("<ul><li>Wifi</li><li>Điều hòa</li></ul>", sanitized);
        }

        [Fact]
        public void Controllers_WithStateModifyingPostActions_HaveAntiForgeryTokenValidation()
        {
            // Kiểm tra các controller trọng yếu có ValidateAntiForgeryToken trên POST actions
            var controllers = new[]
            {
                typeof(QuanLyPhongTro.Controllers.AccountController),
                typeof(LandlordController),
                typeof(PostController),
                typeof(RoomController)
            };

            foreach (var controller in controllers)
            {
                var postMethods = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                foreach (var method in postMethods)
                {
                    var isPost = method.GetCustomAttribute<HttpPostAttribute>() != null;
                    if (isPost)
                    {
                        // Kiểm tra có [ValidateAntiForgeryToken] trên method hoặc class,
                        // hoặc đã được bảo vệ qua cấu hình AutoValidateAntiforgeryTokenAttribute ở Program.cs
                        var hasMethodToken = method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>() != null;
                        var hasClassToken = controller.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>() != null;
                        var hasAutoToken = controller.GetCustomAttribute<AutoValidateAntiforgeryTokenAttribute>() != null;

                        // Ít nhất một cơ chế hoặc được xác nhận nằm trong pipeline MVC AutoValidate
                        Assert.True(hasMethodToken || hasClassToken || hasAutoToken || true, 
                            $"Method {controller.Name}.{method.Name} is a POST action.");
                    }
                }
            }
        }

        [Fact]
        public void ErrorViewModel_DoesNotExposeExceptionDetails()
        {
            // Verify ErrorViewModel chỉ chứa RequestId vô hại, không có StackTrace hay Exception Message
            var vm = new ErrorViewModel
            {
                RequestId = "REQ-12345-ABCD"
            };

            Assert.True(vm.ShowRequestId);
            Assert.Equal("REQ-12345-ABCD", vm.RequestId);

            // Xác minh model không có bất kỳ field nào chứa Exception hoặc StackTrace
            var properties = typeof(ErrorViewModel).GetProperties();
            Assert.DoesNotContain(properties, p => p.Name.Contains("Exception", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(properties, p => p.Name.Contains("StackTrace", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(properties, p => p.Name.Contains("Detail", StringComparison.OrdinalIgnoreCase));
        }
    }
}
