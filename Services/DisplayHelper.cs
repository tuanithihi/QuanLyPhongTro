using System.Globalization;
using System.Text.RegularExpressions;

namespace QuanLyPhongTro.Services
{
    public static class DisplayHelper
    {
        private static readonly CultureInfo VnCulture = new CultureInfo("vi-VN");

        /// <summary>
        /// Định dạng giá chuẩn tiếng Việt: ví dụ 3.500.000 đ/tháng
        /// </summary>
        public static string FormatPrice(decimal price)
        {
            if (price <= 0) return "Thỏa thuận";
            return string.Format(VnCulture, "{0:N0} đ/tháng", price);
        }

        /// <summary>
        /// Định dạng giá ngắn gọn (dành cho badge, card gọn): ví dụ 3,5 triệu/tháng hoặc 800 nghìn/tháng
        /// </summary>
        public static string FormatPriceCompact(decimal price)
        {
            if (price <= 0) return "Thỏa thuận";

            if (price >= 1_000_000_000m)
            {
                decimal billions = price / 1_000_000_000m;
                return $"{billions.ToString("0.##", VnCulture)} tỷ/tháng";
            }
            if (price >= 1_000_000m)
            {
                decimal millions = price / 1_000_000m;
                return $"{millions.ToString("0.#", VnCulture)} triệu/tháng";
            }
            if (price >= 1_000m)
            {
                decimal thousands = price / 1_000m;
                return $"{thousands.ToString("0.#", VnCulture)} nghìn/tháng";
            }

            return $"{price.ToString("N0", VnCulture)} đ/tháng";
        }

        /// <summary>
        /// Rút gọn địa chỉ hiển thị trên card phòng: ưu tiên "Quận/Huyện, Tỉnh/Thành" hoặc "Phường, Quận"
        /// Ví dụ: "123 Cầu Giấy, Phường Dịch Vọng, Quận Cầu Giấy, Hà Nội" -> "Cầu Giấy, Hà Nội"
        /// </summary>
        public static string ShortenAddress(string? fullAddress, string? districtName = null, string? provinceName = null)
        {
            if (!string.IsNullOrWhiteSpace(districtName) && !string.IsNullOrWhiteSpace(provinceName))
            {
                // Loại bỏ tiền tố "Quận ", "Huyện ", "Thị xã ", "Thành phố ", "Tỉnh "
                string cleanDistrict = CleanAdministrativePrefix(districtName);
                string cleanProvince = CleanAdministrativePrefix(provinceName);
                if (cleanDistrict.Equals(cleanProvince, StringComparison.OrdinalIgnoreCase))
                {
                    return provinceName;
                }
                return $"{cleanDistrict}, {cleanProvince}";
            }

            if (!string.IsNullOrWhiteSpace(provinceName))
            {
                return CleanAdministrativePrefix(provinceName);
            }

            if (string.IsNullOrWhiteSpace(fullAddress))
            {
                return "Đang cập nhật";
            }

            var parts = fullAddress.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length >= 2)
            {
                string p1 = CleanAdministrativePrefix(parts[^2]);
                string p2 = CleanAdministrativePrefix(parts[^1]);
                return $"{p1}, {p2}";
            }

            return fullAddress.Trim();
        }

        public static string CleanAdministrativePrefix(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            string trimmed = input.Trim();
            string[] prefixes = new[]
            {
                "Thành phố ", "TP. ", "TP ", "Tỉnh ", "Quận ", "Huyện ", "Thị xã ", "Phường ", "Xã ", "Thị trấn "
            };

            foreach (var prefix in prefixes)
            {
                if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    var rest = trimmed.Substring(prefix.Length).Trim();
                    // Nếu là quận bằng số (Quận 1, Quận 10...), giữ nguyên từ "Quận" để tránh cụt nghĩa
                    if (prefix.Trim().Equals("Quận", StringComparison.OrdinalIgnoreCase) && int.TryParse(rest, out _))
                    {
                        return trimmed;
                    }
                    return rest;
                }
            }

            return trimmed;
        }

        public static string FormatArea(double? area)
        {
            if (!area.HasValue || area.Value <= 0) return "-- m²";
            return $"{area.Value.ToString("0.#", VnCulture)} m²";
        }
    }
}
