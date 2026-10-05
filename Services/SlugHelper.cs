namespace QuanLyPhongTro.Services
{
    public static class SlugHelper
    {
        public static string ToSlug(string text) => GenerateSlug(text);

        public static string GenerateSlug(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            text = text.Trim().ToLowerInvariant();

            // Thay thế ký tự tiếng Việt
            string[] vietnameseSigns = new string[]
            {
                "aAeEoOuUiIdDyY",
                "áàạảãâấầậẩẫăắằặẳẵ",
                "ÁÀẠẢÃÂẤẦẬẨẪĂẮẰẶẲẴ",
                "éèẹẻẽêếềệểễ",
                "ÉÈẸẺẼÊẾỀỆỂỄ",
                "óòọỏõôốồộổỗơớờợởỡ",
                "ÓÒỌỎÕÔỐỒỘỔỖƠỚỜỢỞỠ",
                "úùụủũưứừựửữ",
                "ÚÙỤỦŨƯỨỪỰỬỮ",
                "íìịỉĩ",
                "ÍÌỊỈĨ",
                "đ",
                "Đ",
                "ýỳỵỷỹ",
                "ÝỲỴỶỸ"
            };

            for (int i = 1; i < vietnameseSigns.Length; i++)
            {
                for (int j = 0; j < vietnameseSigns[i].Length; j++)
                {
                    text = text.Replace(vietnameseSigns[i][j], vietnameseSigns[0][i - 1]);
                }
            }

            // Loại bỏ các ký tự đặc biệt không phải chữ/số/dấu gạch
            var sb = new System.Text.StringBuilder();
            bool prevDash = false;
            foreach (char c in text)
            {
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
                {
                    sb.Append(c);
                    prevDash = false;
                }
                else if (c == ' ' || c == '-' || c == '_' || c == '.')
                {
                    if (!prevDash && sb.Length > 0)
                    {
                        sb.Append('-');
                        prevDash = true;
                    }
                }
            }

            string result = sb.ToString().Trim('-');
            return string.IsNullOrEmpty(result) ? "item" : result;
        }
    }
}
