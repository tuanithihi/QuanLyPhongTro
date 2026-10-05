using System.Text.RegularExpressions;

namespace QuanLyPhongTro.Services
{
    /// <summary>
    /// Tiện ích lọc và làm sạch mã độc XSS khỏi nội dung HTML (Summernote, mô tả phòng, v.v.)
    /// </summary>
    public static class HtmlSanitizerHelper
    {
        // Danh sách các tag nguy hiểm tuyệt đối cấm
        private static readonly Regex DangerousTagsRegex = new(
            @"<\s*(script|iframe|embed|object|applet|meta|link|style|form|input|button|base)[^>]*>.*?<\s*/\s*\1\s*>|<\s*(script|iframe|embed|object|applet|meta|link|style|form|input|button|base)[^>]*\/?>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        // Các sự kiện inline: onload=, onclick=, onerror=, onmouseover=, ...
        private static readonly Regex InlineEventRegex = new(
            @"\s+on[a-z]+\s*=\s*(?:'[^']*'|""[^""]*""|[^\s>]+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Các giao thức nguy hiểm trong href/src: javascript:, vbscript:, data:text/html
        private static readonly Regex DangerousProtocolDoubleQuote = new(
            @"\s*(href|src)\s*=\s*""[^""]*?(?:javascript|vbscript|data\s*:\s*text\/html)[^""]*""",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex DangerousProtocolSingleQuote = new(
            @"\s*(href|src)\s*=\s*'[^']*?(?:javascript|vbscript|data\s*:\s*text\/html)[^']*'",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex DangerousProtocolNoQuote = new(
            @"\s*(href|src)\s*=\s*(?:javascript|vbscript|data\s*:\s*text\/html)[^\s>]*",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string SanitizeHtml(string? rawHtml) => Sanitize(rawHtml);

        /// <summary>
        /// Làm sạch chuỗi HTML, loại bỏ thẻ độc hại và sự kiện inline.
        /// </summary>
        public static string Sanitize(string? rawHtml)
        {
            if (string.IsNullOrWhiteSpace(rawHtml))
                return string.Empty;

            var cleaned = rawHtml;

            // 1. Loại bỏ các thẻ script, iframe, object, v.v.
            cleaned = DangerousTagsRegex.Replace(cleaned, string.Empty);

            // 2. Loại bỏ các sự kiện inline (onclick, onerror, onload, ...)
            cleaned = InlineEventRegex.Replace(cleaned, string.Empty);

            // 3. Loại bỏ giao thức javascript:, vbscript: trong thuộc tính href/src
            cleaned = DangerousProtocolDoubleQuote.Replace(cleaned, string.Empty);
            cleaned = DangerousProtocolSingleQuote.Replace(cleaned, string.Empty);
            cleaned = DangerousProtocolNoQuote.Replace(cleaned, string.Empty);

            return cleaned.Trim();
        }
    }
}
