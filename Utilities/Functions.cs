using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyThuVien.Utilities
{
    public class Functions
    {
        // Session variables
        public static int _UserID = 0;
        public static string _UserName = string.Empty;
        public static string _FullName = string.Empty;
        public static string _Email = string.Empty;
        public static string _Role = string.Empty;
        public static string _Message = string.Empty;
        public static string _Avatar = string.Empty;

        // Deprecated - giữ lại để tương thích
        public static bool _IsAdmin = false;

        /// <summary>
        /// Kiểm tra user đã đăng nhập chưa
        /// </summary>
        public static bool IsLogin()
        {
            return _UserID > 0 && !string.IsNullOrEmpty(_UserName);
        }

        /// <summary>
        /// Kiểm tra user có phải Admin không
        /// </summary>
        public static bool IsAdmin()
        {
            return IsLogin() && _Role?.ToLower() == "admin";
        }

        /// <summary>
        /// Kiểm tra user có phải User thông thường không
        /// </summary>
        public static bool IsUser()
        {
            return IsLogin() && _Role?.ToLower() == "user";
        }

        /// <summary>
        /// Reset tất cả session variables
        /// </summary>
        public static void ClearSession()
        {
            _UserID = 0;
            _UserName = string.Empty;
            _FullName = string.Empty;
            _Email = string.Empty;
            _Role = string.Empty;
            _Avatar = string.Empty;
            _Message = string.Empty;
            _IsAdmin = false;
        }

        public static string TitleSlugGeneration(string type, string? title, long id)
        {
            return type + "-" + SlugGenerator.SlugGenerator.GenerateSlug(title) + "-" + id.ToString() + ".html";
        }

        public static string MD5Hash(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            using (MD5 md5 = MD5.Create())
            {
                byte[] result = md5.ComputeHash(Encoding.ASCII.GetBytes(text));

                StringBuilder strBuilder = new StringBuilder();
                for (int i = 0; i < result.Length; i++)
                {
                    strBuilder.Append(result[i].ToString("x2"));
                }
                return strBuilder.ToString();
            }
        }

        public static string MD5Password(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            string str = MD5Hash(text);
            for (int i = 0; i < 5; i++)
            {
                str = MD5Hash(str + str);
            }
            return str;
        }

        public static string getCurrentDate()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}
