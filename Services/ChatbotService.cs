// ChatbotService.cs - PHIÊN BẢN HOÀN CHỈNH (ĐÃ FIX LỖI)
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Models;
using Microsoft.Extensions.Caching.Memory;

namespace QuanLyThuVien.Services
{
    public class ChatbotService
    {
        private readonly DataContext _context;
        private readonly IConfiguration _config;
        private readonly HttpClient _http;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ChatbotService> _logger;

        public ChatbotService(
            DataContext context, 
            IConfiguration config, 
            IMemoryCache cache,
            ILogger<ChatbotService> logger,
            HttpClient http)
        {
            _context = context;
            _config = config;
            _cache = cache;
            _logger = logger;
            _http = http;
            
            // KHÔNG khởi tạo cache trong constructor để tránh concurrency issue
            // Cache sẽ được khởi tạo khi cần thiết trong các phương thức
        }

        public async Task<string> Ask(string userMessage, int? userId = null, string? userName = null)
        {
            _logger.LogInformation("User asked: {Message}, UserID: {UserId}", userMessage, userId);
            
            try
            {
                // Lấy dữ liệu THẬT từ database (bao gồm dữ liệu của user nếu đã đăng nhập)
                var realData = await GetRealDatabaseData(userMessage, userId);
                
                // Tạo prompt với chỉ dẫn cụ thể (có thông tin user nếu đã đăng nhập)
                var systemPrompt = BuildStrictPrompt(realData, userMessage, userId, userName);
                
                _logger.LogDebug("System prompt length: {Length}", systemPrompt.Length);
                
                // FIX 3: Gửi với timeout ngắn hơn
                var response = await SendToAIWithTimeout(systemPrompt, userMessage, 30);
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Ask method");
                return $"Xin lỗi, có lỗi hệ thống: {ex.Message}. Vui lòng thử lại sau.";
            }
        }

        private async Task<Dictionary<string, object>> GetRealDatabaseData(string userMessage, int? userId = null)
        {
            var data = new Dictionary<string, object>();
            
            // LẤY FULL DỮ LIỆU TỪ DATABASE - KHÔNG PHỤ THUỘC KEYWORD
            // THỰC HIỆN CÁC QUERIES TUẦN TỰ để tránh DbContext concurrency issue
            
            // 1. Thống kê tổng quan
            data["thong_ke_thuc_te"] = await GetRealStatistics();
            
            // 2. Dữ liệu sách (giảm số lượng để tránh prompt quá dài)
            // KHÔNG lấy tất cả sách - chỉ lấy một số để làm mẫu
            data["sach_moi_nhat"] = await GetLatestBooks(5);
            data["sach_pho_bien"] = await GetTopBorrowedBooks(5);
            
            // 3. Thể loại
            data["tat_ca_the_loai"] = await GetCategoriesWithCounts();
            
            // 4. Tác giả
            data["tat_ca_tac_gia"] = await GetAllAuthors();
            data["tac_gia_hang_dau"] = await GetTopAuthors();
            
            // 5. Nhà xuất bản
            data["tat_ca_nha_xuat_ban"] = await GetAllPublishers();
            
            // 6. Mượn trả (tổng quan)
            data["tinh_trang_muon_tra"] = await GetBorrowStatus();
            data["muon_qua_han"] = await GetOverdueBorrows();
            data["quy_dinh_muon"] = GetBorrowRules();
            
            // 7. Người dùng (thống kê)
            data["thong_ke_nguoi_dung"] = await GetUserStatistics();
            
            // 8. Bài viết/Blog (nếu có)
            data["bai_viet_moi"] = await GetRecentPosts(5);
            
            // 9. DỮ LIỆU CỦA USER ĐÃ ĐĂNG NHẬP (nếu có)
            if (userId.HasValue && userId.Value > 0)
            {
                data["nguoi_dung_dang_nhap"] = true;
                data["thong_tin_nguoi_dung"] = await GetCurrentUserInfo(userId.Value);
                data["sach_dang_muon_cua_toi"] = await GetUserBorrowedBooks(userId.Value);
                data["lich_su_muon_cua_toi"] = await GetUserBorrowHistory(userId.Value);
            }
            else
            {
                data["nguoi_dung_dang_nhap"] = false;
            }
            
            return data;
        }

        private async Task<Dictionary<string, object>> GetRealStatistics()
        {
            try
            {
                // ĐẢM BẢO query database thật
                var stats = new Dictionary<string, object>();
                
                // Query 1: Thống kê sách (tách rõ số cuốn sách và số lượng tồn kho)
                // Tổng số cuốn sách khác nhau (số tên sách)
                stats["tong_so_cuon_sach"] = await _context.Books.CountAsync(b => b.IsActive == true);
                // Tổng số lượng sách tồn kho (tổng Quantity của tất cả sách)
                stats["tong_so_luong_ton_kho"] = await _context.Books
                    .Where(b => b.IsActive == true)
                    .SumAsync(b => (int?)b.Quantity) ?? 0;
                // Giữ lại để tương thích
                stats["tong_sach"] = stats["tong_so_cuon_sach"];
                stats["sach_dang_hoat_dong"] = stats["tong_so_cuon_sach"];
                stats["sach_co_san"] = await _context.Books.Where(b => b.IsActive == true && b.Quantity > 0).CountAsync();
                
                // Query 2: Thể loại
                stats["so_the_loai"] = await _context.Categories.CountAsync(c => c.IsActive);
                
                // Query 3: Tác giả
                stats["so_tac_gia"] = await _context.Authors.CountAsync(a => a.IsActive == true);
                
                // Query 4: Mượn trả
                stats["dang_muon"] = await _context.Borrows.CountAsync(b => b.Status == "Borrowing");
                stats["muon_hom_nay"] = await _context.Borrows
                    .CountAsync(b => b.BorrowDate.Date == DateTime.Today);
                
                // Query 5: Top thể loại
                var topCategories = await _context.Books
                    .Include(b => b.Category)
                    .Where(b => b.IsActive == true)
                    .GroupBy(b => b.Category != null ? b.Category.CategoryName : "Không phân loại")
                    .Select(g => new { Category = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(5)
                    .ToListAsync();
                
                stats["the_loai_pho_bien"] = topCategories;
                
                _logger.LogInformation("Real stats fetched: {Stats}", JsonSerializer.Serialize(stats));
                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting real statistics");
                return new Dictionary<string, object> { { "error", "Không lấy được dữ liệu" } };
            }
        }

        // ĐÃ THÊM: Method GetBorrowStatus đã thiếu
        private async Task<Dictionary<string, object>> GetBorrowStatus()
        {
            var status = new Dictionary<string, object>();
            
            status["tong_phieu_muon"] = await _context.Borrows.CountAsync();
            status["dang_muon"] = await _context.Borrows.CountAsync(b => b.Status == "Borrowing");
            status["da_tra"] = await _context.Borrows.CountAsync(b => b.Status == "Returned");
            status["qua_han"] = await _context.Borrows
                .CountAsync(b => b.Status == "Borrowing" && b.DueDate < DateTime.Today);
            
            // Thêm chi tiết 5 phiếu mượn gần nhất
            var recentBorrows = await _context.Borrows
                .Include(b => b.User)
                .Include(b => b.BorrowDetails!)
                    .ThenInclude(bd => bd.Book)
                .OrderByDescending(b => b.BorrowDate)
                .Take(5)
                .Select(b => new
                {
                    User = b.User != null ? b.User.FullName : "Không rõ",
                    BorrowDate = b.BorrowDate.ToString("dd/MM/yyyy"),
                    DueDate = b.DueDate.ToString("dd/MM/yyyy"),
                    Status = b.Status,
                    BookCount = b.BorrowDetails != null ? b.BorrowDetails.Count : 0
                })
                .ToListAsync();
            
            status["phieu_muon_gan_nhat"] = recentBorrows;
            
            return status;
        }

        // ĐÃ THÊM: Method GetBookDetails đã thiếu
        private async Task<List<BookDetailInfo>> GetBookDetails()
        {
            // Load về memory trước để tránh lỗi LINQ translation
            var books = await _context.Books
                .Include(b => b.Category)
                .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                .Include(b => b.Publisher)
                .Where(b => b.IsActive == true)
                .OrderByDescending(b => b.CreatedAt)
                .Take(15)
                .ToListAsync();
            
            return books.Select(b => new BookDetailInfo
            {
                BookID = b.BookID,
                Title = b.Title ?? "Không có tiêu đề",
                Description = "", // Bỏ mô tả để giảm độ dài prompt
                Author = b.BookAuthors.FirstOrDefault()?.Author?.AuthorName ?? "Không rõ",
                Category = b.Category?.CategoryName ?? "Không phân loại",
                Publisher = b.Publisher?.PublisherName ?? "Không rõ",
                Year = b.PublishedYear,
                Quantity = b.Quantity,
                Available = b.Quantity > 0 ? "Có sẵn" : "Hết sách",
                CreatedAt = b.CreatedAt.ToString("dd/MM/yyyy")
            }).ToList();
        }

        private async Task<List<BookInfo>> GetLatestBooks(int count)
        {
            // Load về memory trước để tránh lỗi LINQ translation
            var books = await _context.Books
                .Include(b => b.Category)
                .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                .Where(b => b.IsActive == true)
                .OrderByDescending(b => b.CreatedAt)
                .Take(count)
                .ToListAsync();
            
            return books.Select(b => new BookInfo
            {
                BookID = b.BookID,
                Title = b.Title ?? "Không có tiêu đề",
                Description = "", // Bỏ mô tả để giảm độ dài prompt
                Author = b.BookAuthors.FirstOrDefault()?.Author?.AuthorName ?? "Không rõ",
                Category = b.Category?.CategoryName ?? "Không phân loại",
                Year = b.PublishedYear,
                Available = b.Quantity > 0 ? "Có sẵn" : "Hết sách"
            }).ToList();
        }

        private async Task<List<CategoryInfo>> GetCategoriesWithCounts()
        {
            var categories = await _context.Categories
                .Where(c => c.IsActive)
                .ToListAsync();

            var bookCounts = await _context.Books
                .Where(b => b.IsActive == true)
                .GroupBy(b => b.CategoryID)
                .Select(g => new { CategoryID = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.CategoryID, x => x.Count);

            return categories.Select(c => new CategoryInfo
            {
                Name = c.CategoryName ?? "Không tên",
                BookCount = bookCounts.ContainsKey(c.CategoryID) ? bookCounts[c.CategoryID] : 0,
                Description = c.Description ?? "Không có mô tả"
            })
            .ToList();
        }

        private string BuildStrictPrompt(Dictionary<string, object> realData, string userMessage, int? userId = null, string? userName = null)
        {
            var prompt = new StringBuilder();
            
            // QUAN TRỌNG: Chỉ dẫn NGHIÊM NGẶT cho AI với các câu hỏi mẫu
            prompt.AppendLine(@"BẠN LÀ CHATBOT THƯ VIỆN LIBRAHUB - Trợ lý ảo thông minh.

=== QUY TẮC QUAN TRỌNG ===
1. Sử dụng DỮ LIỆU THỰC bên dưới để trả lời, KHÔNG bịa ra số liệu
2. Trả lời bằng TIẾNG VIỆT, ngắn gọn (3-5 câu), thân thiện, chuyên nghiệp
3. TUYỆT ĐỐI KHÔNG dùng markdown (**), xuống dòng hợp lý
4. KHÔNG đưa link/URL, chỉ hướng dẫn bằng lời
5. Gọi người dùng là 'bạn', dùng emoji phù hợp (📚 💡 ✨ 🎯)

=== CÁC TRANG CHÍNH ===
- Danh sách sách: /Book/Index
- Mượn sách: /Borrow/Borrow (yêu cầu đăng nhập)
- Lịch sử mượn: /Borrow/History (yêu cầu đăng nhập)
- Đăng nhập: /Account/Login
- Chi tiết sách: /Book/Details/{id}
- Danh mục: /Category/Index
- Tác giả: /Author/Index

=== MẪU CÂU HỎI & CÁCH TRẢ LỜI ===");

            // Thêm câu hỏi mẫu theo context
            if (userId.HasValue && userId.Value > 0 && !string.IsNullOrEmpty(userName))
            {
                prompt.AppendLine($@"
📌 NGƯỜI DÙNG: {userName} (ID: {userId}) - ĐÃ ĐĂNG NHẬP

CÂU HỎI MẪU KHI ĐÃ ĐĂNG NHẬP:
1. 'Tôi đang mượn sách gì?' → Kiểm tra dữ liệu sach_dang_muon_cua_toi, trả về danh sách sách đang mượn với ngày hạn trả
2. 'Lịch sử mượn của tôi?' → Kiểm tra lich_su_muon_cua_toi, tóm tắt số lần mượn, sách hay mượn
3. 'Tôi có sách quá hạn không?' → Kiểm tra IsOverdue trong sach_dang_muon_cua_toi
4. 'Tôi có thể mượn thêm sách không?' → Kiểm tra số sách đang mượn, thông báo có thể mượn thêm
5. 'Giới thiệu sách mới cho tôi' → Dựa vào lịch sử mượn để gợi ý sách cùng thể loại/tác giả
6. 'Tài khoản tôi?' → Hiển thị thông tin từ thong_tin_nguoi_dung
7. 'Tôi mượn nhiều sách nhất thể loại nào?' → Phân tích lich_su_muon_cua_toi

CÁCH TRẢ LỜI:
- Gọi tên người dùng: 'Chào {userName}!'
- Cá nhân hóa: 'Bạn đang mượn X cuốn sách...'
- Nhắc nhở: 'Lưu ý: Sách [tên] sắp đến hạn trả vào [ngày]'
");
            }
            else
            {
                prompt.AppendLine(@"
⚠️ NGƯỜI DÙNG: CHƯA ĐĂNG NHẬP

CÂU HỎI MẪU KHI CHƯA ĐĂNG NHẬP:
1. 'Thư viện có bao nhiêu sách?' → Trả về thong_ke_sach từ dữ liệu
2. 'Có sách về [chủ đề] không?' → Tìm trong danh_sach_sach hoặc the_loai
3. 'Sách mới nhất?' → Lấy từ sach_moi_nhat
4. 'Thể loại nào nhiều sách nhất?' → Dùng dữ liệu the_loai_va_so_luong
5. 'Tôi muốn mượn sách' → Hướng dẫn đăng nhập: 'Vui lòng đăng nhập để mượn sách'
6. 'Tôi đang mượn sách gì?' → 'Bạn cần đăng nhập để xem sách đang mượn'
7. 'Làm sao để đăng ký?' → Hướng dẫn: 'Vào trang Đăng nhập, chọn Đăng ký tài khoản mới'
8. 'Thư viện mở cửa lúc nào?' → 'Thư viện mở cửa 24/7, bạn có thể truy cập và đọc sách online bất cứ lúc nào'
9. 'Giới thiệu tác giả [tên]?' → Tìm trong danh_sach_tac_gia
10. 'Top sách hay nhất?' → Dựa vào thong_ke_muon hoặc sach_pho_bien

CÁCH TRẢ LỜI:
- Thân thiện: 'Chào bạn! Thư viện LibraHub có...'
- Hướng dẫn rõ ràng khi cần đăng nhập
- Gợi ý: 'Bạn có thể đăng nhập để trải nghiệm đầy đủ tính năng'
");
            }

            prompt.AppendLine(@"
=== HƯỚNG DẪN TRẢ LỜI ===
CÁC LOẠI CÂU HỎI CHÍNH:
• Thống kê: Dùng thong_ke_sach, thong_ke_muon_tra
• Tìm sách: Dùng sach_moi_nhat, sach_pho_bien, lọc theo tên/tác giả/thể loại
• Tác giả/NXB: Dùng tat_ca_tac_gia, tac_gia_hang_dau, tat_ca_nha_xuat_ban
• Thể loại: Dùng tat_ca_the_loai, the_loai_pho_bien
• Mượn trả: Dùng tinh_trang_muon_tra, muon_qua_han, quy_dinh_muon (Tối đa 3 sách/người, 14 ngày)
• Người dùng đã đăng nhập: Dùng sach_dang_muon_cua_toi, lich_su_muon_cua_toi
• Hệ thống: Hướng dẫn đăng nhập, mượn sách, xem PDF, tìm kiếm

LƯU Ý:
- Phân biệt: tong_so_cuon_sach (số tên sách) vs tong_so_luong_ton_kho (tổng số bản)
- Nếu không biết: 'Mình chưa có thông tin này, bạn liên hệ thủ thư'

=== DỮ LIỆU THỰC TỪ DATABASE ===
Cập nhật: " + DateTime.Now.ToString("HH:mm dd/MM/yyyy"));
            
            // Thông báo rõ ràng về trạng thái đăng nhập
            if (userId.HasValue && userId.Value > 0)
            {
                prompt.AppendLine($"\n✅ Người dùng {userName} đã đăng nhập - Có thể truy cập thông tin cá nhân");
            }
            else
            {
                prompt.AppendLine("\n❌ Người dùng CHƯA đăng nhập - Chỉ trả lời câu hỏi chung, hướng dẫn đăng nhập khi cần");
            }
            
            prompt.AppendLine("\n=== DỮ LIỆU CẬP NHẬT LÚC " + DateTime.Now.ToString("HH:mm dd/MM") + " ===");
            
            // Thêm dữ liệu thực với định dạng rõ ràng
            foreach (var item in realData)
            {
                prompt.AppendLine($"\n📌 {item.Key.ToUpper()}:");
                
                if (item.Value is Dictionary<string, object> dict)
                {
                    foreach (var kv in dict)
                    {
                        // Kiểm tra nếu giá trị là List hoặc Array
                        if (kv.Value is System.Collections.IEnumerable enumerable && 
                            !(kv.Value is string))
                        {
                            prompt.Append($"  • {kv.Key}: ");
                            var items = new List<string>();
                            foreach (var element in enumerable)
                            {
                                items.Add(element?.ToString() ?? "null");
                            }
                            prompt.AppendLine(string.Join(", ", items));
                        }
                        else
                        {
                            prompt.AppendLine($"  • {kv.Key}: {kv.Value}");
                        }
                    }
                }
                else if (item.Value is List<BookInfo> books && books.Any())
                {
                    // Chỉ hiển thị 5 cuốn đầu để giảm độ dài prompt
                    foreach (var book in books.Take(5))
                    {
                        prompt.AppendLine($"  • Sách ID {book.BookID}: {book.Title} - Tác giả: {book.Author} | Thể loại: {book.Category} | Năm: {book.Year}");
                    }
                    if (books.Count > 5)
                        prompt.AppendLine($"  ... và {books.Count - 5} cuốn sách khác");
                }
                else if (item.Value is List<CategoryInfo> categories && categories.Any())
                {
                    foreach (var cat in categories.Take(5))
                    {
                        prompt.AppendLine($"  • {cat.Name}: {cat.BookCount} sách");
                    }
                }
                else if (item.Value is List<BookDetailInfo> bookDetails && bookDetails.Any())
                {
                    // Hiển thị sách với thông tin gọn gàng (chỉ lấy 5 cuốn đầu để giảm độ dài)
                    foreach (var book in bookDetails.Take(5))
                    {
                        prompt.AppendLine($"  • Sách ID {book.BookID}: {book.Title} - Tác giả: {book.Author} | Thể loại: {book.Category} | Năm: {book.Year} | Số lượng: {book.Quantity}");
                    }
                    if (bookDetails.Count > 5)
                        prompt.AppendLine($"  ... và {bookDetails.Count - 5} cuốn sách khác");
                }
                else if (item.Value is List<AuthorDetailInfo> authors && authors.Any())
                {
                    // Chỉ hiển thị 5 tác giả đầu để giảm độ dài
                    foreach (var author in authors.Take(5))
                    {
                        prompt.AppendLine($"  • Tác giả ID {author.AuthorID}: {author.Name} - Email: {author.Email} | Số sách: {author.BookCount}");
                    }
                    if (authors.Count > 5)
                        prompt.AppendLine($"  ... và {authors.Count - 5} tác giả khác");
                }
                else if (item.Value is List<PublisherInfo> publishers && publishers.Any())
                {
                    // Chỉ hiển thị 5 nhà xuất bản đầu
                    foreach (var pub in publishers.Take(5))
                    {
                        prompt.AppendLine($"  • {pub.Name} - {pub.Email}");
                    }
                    if (publishers.Count > 5)
                        prompt.AppendLine($"  ... và {publishers.Count - 5} nhà xuất bản khác");
                }
                else if (item.Value is List<PostInfo> posts && posts.Any())
                {
                    foreach (var post in posts)
                    {
                        prompt.AppendLine($"  • {post.Title} - {post.Author} - {post.MenuName} ({post.CreatedDate})");
                    }
                }
                else if (item.Key == "sach_dang_muon_cua_toi" && item.Value is List<UserBorrowInfo> currentBorrows && currentBorrows.Any())
                {
                    prompt.AppendLine($"  Tổng số sách đang mượn: {currentBorrows.Sum(b => b.Quantity)} cuốn");
                    foreach (var borrow in currentBorrows)
                    {
                        prompt.AppendLine($"  • Sách ID {borrow.BookID}: {borrow.BookTitle} (x{borrow.Quantity})");
                        prompt.AppendLine($"    Tác giả: {borrow.Author} | Thể loại: {borrow.Category}");
                        prompt.AppendLine($"    Ngày mượn: {borrow.BorrowDate:dd/MM/yyyy} | Hạn trả: {borrow.DueDate:dd/MM/yyyy}");
                        if (borrow.IsOverdue)
                            prompt.AppendLine($"    ⚠️ QUÁ HẠN {borrow.DaysOverdue} ngày");
                        prompt.AppendLine();
                    }
                }
                else if (item.Key == "lich_su_muon_cua_toi" && item.Value is List<UserBorrowHistoryInfo> history && history.Any())
                {
                    prompt.AppendLine($"  Tổng số phiếu mượn: {history.Count}");
                    foreach (var borrow in history.Take(5))
                    {
                        prompt.AppendLine($"  • Phiếu mượn #{borrow.BorrowID} - Ngày mượn: {borrow.BorrowDate:dd/MM/yyyy} - Hạn trả: {borrow.DueDate:dd/MM/yyyy}");
                        prompt.AppendLine($"    Trạng thái: {borrow.Status}");
                        if (borrow.IsOverdue)
                            prompt.AppendLine($"    ⚠️ QUÁ HẠN");
                        prompt.AppendLine($"    Sách: {string.Join(", ", borrow.Books)}");
                        prompt.AppendLine();
                    }
                }
                else
                {
                    // Format dữ liệu dictionary hoặc object khác
                    var jsonString = JsonSerializer.Serialize(item.Value);
                    if (jsonString.Length > 500)
                    {
                        prompt.AppendLine($"  {jsonString.Substring(0, 500)}... (còn tiếp)");
                    }
                    else
                    {
                        prompt.AppendLine($"  {jsonString}");
                    }
                }
            }
            
            // Hướng dẫn ngắn gọn
            prompt.AppendLine("\n=== KẾT LUẬN ===");
            prompt.AppendLine("Dùng dữ liệu trên để trả lời CHÍNH XÁC, ngắn gọn (3-5 câu), KHÔNG dùng **, KHÔNG link.");
            
            prompt.AppendLine("\n=== CÂU HỎI CỦA NGƯỜI DÙNG ===");
            prompt.AppendLine(userMessage);
            prompt.AppendLine("\n=== BẮT ĐẦU TRẢ LỜI ===");
            
            return prompt.ToString();
        }

        private async Task<string> SendToAIWithTimeout(string systemPrompt, string userMessage, int timeoutSeconds)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                
                var model = _config["OpenRouter:Model"] ?? "deepseek/deepseek-r1-0528:free";
                var requestData = new
                {
                    model = model,
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userMessage }
                    },
                    temperature = 0.1,
                    max_tokens = 2000
                };
                
                var content = new StringContent(
                    JsonSerializer.Serialize(requestData),
                    Encoding.UTF8,
                    "application/json");
                
                // Tạo request message với headers riêng để tránh xung đột
                var request = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions")
                {
                    Content = content
                };
                
                request.Headers.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue(
                        "Bearer", _config["OpenRouter:ApiKey"]);
                
                // Thêm headers bắt buộc cho OpenRouter
                var referer = _config["OpenRouter:Referer"] ?? "https://librahub.local";
                request.Headers.Add("HTTP-Referer", referer);
                request.Headers.Add("X-Title", "LibraHub");
                
                var response = await _http.SendAsync(request, cts.Token);
                
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("OpenRouter error: {StatusCode} - {Error}", response.StatusCode, error);
                    return "Hiện không kết nối được với AI. Vui lòng thử lại sau.";
                }
                
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                
                return doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "Không nhận được phản hồi.";
            }
            catch (TaskCanceledException)
            {
                return "Câu hỏi quá phức tạp hoặc hệ thống bận. Vui lòng hỏi câu ngắn hơn.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling OpenRouter");
                return "Lỗi kết nối AI. Vui lòng thử lại.";
            }
        }

        // Các method hỗ trợ
        private async Task<List<BorrowInfo>> GetOverdueBorrows()
        {
            // Load về memory trước để tránh lỗi LINQ translation
            var borrows = await _context.Borrows
                .Include(b => b.User)
                .Include(b => b.BorrowDetails!)
                    .ThenInclude(bd => bd.Book)
                .Where(b => b.Status == "Borrowing" && b.DueDate < DateTime.Today)
                .OrderBy(b => b.DueDate)
                .Take(5)
                .ToListAsync();
            
            return borrows.Select(b => new BorrowInfo
            {
                UserName = b.User?.FullName ?? "Không rõ",
                DueDate = b.DueDate,
                Books = b.BorrowDetails != null ? 
                    b.BorrowDetails
                        .Where(bd => bd.Book != null)
                        .Select(bd => bd.Book!.Title ?? "Không tên")
                        .ToList() : 
                    new List<string>(),
                DaysOverdue = (DateTime.Today - b.DueDate).Days
            }).ToList();
        }

        private async Task<List<AuthorInfo>> GetTopAuthors()
        {
            // Load authors về memory trước với Include để tránh lỗi LINQ translation
            var authors = await _context.Authors
                .Include(a => a.BookAuthors!)
                    .ThenInclude(ba => ba.Book)
                .Where(a => a.IsActive == true)
                .ToListAsync();
            
            // Xử lý trên memory (LINQ to Objects)
            return authors
                .Select(a => new AuthorInfo
                {
                    Name = a.AuthorName ?? "Không tên",
                    BookCount = a.BookAuthors != null ? a.BookAuthors.Count : 0,
                    LatestBook = a.BookAuthors != null && a.BookAuthors.Any() ?
                        a.BookAuthors
                            .Where(ba => ba.Book != null)
                            .OrderByDescending(ba => ba.Book!.CreatedAt)
                            .Select(ba => ba.Book!.Title)
                            .FirstOrDefault() ?? "Không có sách" : "Không có sách"
                })
                .OrderByDescending(a => a.BookCount)
                .Take(10)
                .ToList();
        }

        private string GetBorrowRules()
        {
            return "Tối đa 3 sách/người, thời hạn 14 ngày, phạt 2.000đ/ngày/sách";
        }

        // THÊM: Lấy TẤT CẢ sách (không giới hạn)
        private async Task<List<BookDetailInfo>> GetAllBooks()
        {
            // Load về memory trước để tránh lỗi LINQ translation
            var books = await _context.Books
                .Include(b => b.Category)
                .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                .Include(b => b.Publisher)
                .Where(b => b.IsActive == true)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
            
            return books.Select(b => new BookDetailInfo
            {
                BookID = b.BookID,
                Title = b.Title ?? "Không có tiêu đề",
                Description = "", // Bỏ mô tả để giảm độ dài prompt
                Author = b.BookAuthors.FirstOrDefault()?.Author?.AuthorName ?? "Không rõ",
                Category = b.Category?.CategoryName ?? "Không phân loại",
                Publisher = b.Publisher?.PublisherName ?? "Không rõ",
                Year = b.PublishedYear,
                Quantity = b.Quantity,
                Available = b.Quantity > 0 ? "Có sẵn" : "Hết sách",
                CreatedAt = b.CreatedAt.ToString("dd/MM/yyyy")
            }).ToList();
        }

        // THÊM: Lấy TẤT CẢ tác giả
        private async Task<List<AuthorDetailInfo>> GetAllAuthors()
        {
            var authors = await _context.Authors
                .Include(a => a.BookAuthors)
                .Where(a => a.IsActive == true)
                .OrderBy(a => a.AuthorName)
                .ToListAsync();
            
            return authors.Select(a => new AuthorDetailInfo
            {
                AuthorID = a.AuthorID,
                Name = a.AuthorName ?? "Không tên",
                Email = a.Email ?? "Không có",
                BookCount = a.BookAuthors != null ? a.BookAuthors.Count : 0,
                Biography = "" // Bỏ tiểu sử để giảm độ dài prompt
            }).ToList();
        }

        // THÊM: Lấy TẤT CẢ nhà xuất bản
        private async Task<List<PublisherInfo>> GetAllPublishers()
        {
            return await _context.Publishers
                .Where(p => p.IsActive == true)
                .OrderBy(p => p.PublisherName)
                .Select(p => new PublisherInfo
                {
                    Name = p.PublisherName ?? "Không tên",
                    Email = p.Email ?? "Không có",
                    Phone = p.Phone ?? "Không có",
                    Address = p.Address ?? "Không có"
                })
                .ToListAsync();
        }

        // THÊM: Sách được mượn nhiều nhất
        private async Task<List<BookInfo>> GetTopBorrowedBooks(int count)
        {
            // Lấy danh sách BookID được mượn nhiều nhất
            var topBorrowed = await _context.BorrowDetails
                .Where(bd => bd.Book != null && bd.Book.IsActive == true)
                .GroupBy(bd => bd.BookID)
                .Select(g => new { BookID = g.Key, BorrowCount = g.Sum(x => x.Quantity) })
                .OrderByDescending(x => x.BorrowCount)
                .Take(count)
                .ToListAsync();

            if (!topBorrowed.Any())
                return new List<BookInfo>();

            var bookIds = topBorrowed.Select(x => x.BookID).ToList();
            var borrowCountDict = topBorrowed.ToDictionary(x => x.BookID, x => x.BorrowCount);
            
            // Load books về memory trước, sau đó order bằng LINQ to Objects
            var books = await _context.Books
                .Include(b => b.Category)
                .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                .Where(b => bookIds.Contains(b.BookID) && b.IsActive == true)
                .ToListAsync();
            
            // Order bằng LINQ to Objects (sau khi đã load về memory)
            return books
                .OrderByDescending(b => borrowCountDict.ContainsKey(b.BookID) ? borrowCountDict[b.BookID] : 0)
                .Select(b => new BookInfo
                {
                    BookID = b.BookID,
                    Title = b.Title ?? "Không có tiêu đề",
                    Description = "", // Bỏ mô tả để giảm độ dài prompt
                    Author = b.BookAuthors.FirstOrDefault()?.Author?.AuthorName ?? "Không rõ",
                    Category = b.Category?.CategoryName ?? "Không phân loại",
                    Year = b.PublishedYear,
                    Available = b.Quantity > 0 ? "Có sẵn" : "Hết sách"
                })
                .ToList();
        }

        // THÊM: Thống kê người dùng
        private async Task<Dictionary<string, object>> GetUserStatistics()
        {
            var stats = new Dictionary<string, object>();
            
            stats["tong_nguoi_dung"] = await _context.Users.CountAsync();
            stats["nguoi_dung_hoat_dong"] = await _context.Users.CountAsync(u => u.IsActive == true);
            stats["nguoi_dung_dang_muon"] = await _context.Borrows
                .Where(b => b.Status == "Borrowing")
                .Select(b => b.UserID)
                .Distinct()
                .CountAsync();
            
            return stats;
        }

        // THÊM: Bài viết mới nhất
        private async Task<List<PostInfo>> GetRecentPosts(int count)
        {
            try
            {
                return await _context.viewPostMenus
                    .Where(p => p.IsActive == true)
                    .OrderByDescending(p => p.CreatedDate)
                    .Take(count)
                    .Select(p => new PostInfo
                    {
                        Title = p.Title ?? "Không có tiêu đề",
                        Author = p.Author ?? "Không rõ",
                        MenuName = p.MenuName ?? "Không phân loại",
                        CreatedDate = p.CreatedDate != null ? p.CreatedDate.Value.ToString("dd/MM/yyyy") : "Không rõ"
                    })
                    .ToListAsync();
            }
            catch
            {
                // Nếu view không tồn tại hoặc lỗi, trả về list rỗng
                return new List<PostInfo>();
            }
        }

        // THÊM: Lấy thông tin user hiện tại
        private async Task<Dictionary<string, object>> GetCurrentUserInfo(int userId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserID == userId);
            
            if (user == null)
                return new Dictionary<string, object>();
            
            return new Dictionary<string, object>
            {
                ["UserID"] = user.UserID,
                ["UserName"] = user.UserName ?? "Không rõ",
                ["FullName"] = user.FullName ?? "Không rõ",
                ["Email"] = user.Email ?? "Không rõ",
                ["Role"] = user.Role ?? "User"
            };
        }

        // THÊM: Lấy sách đang mượn của user (chưa trả)
        private async Task<List<UserBorrowInfo>> GetUserBorrowedBooks(int userId)
        {
            var borrowDetails = await _context.BorrowDetails
                .Include(bd => bd.Borrow)
                .Include(bd => bd.Book)
                    .ThenInclude(b => b.Category)
                .Include(bd => bd.Book)
                    .ThenInclude(b => b.BookAuthors)
                        .ThenInclude(ba => ba.Author)
                .Where(bd => bd.Borrow.UserID == userId 
                    && bd.BorrowStatus != "Returned" 
                    && bd.ReturnDate == null)
                .OrderByDescending(bd => bd.Borrow.BorrowDate)
                .ToListAsync();
            
            return borrowDetails.Select(bd => new UserBorrowInfo
            {
                BookID = bd.BookID,
                BookTitle = bd.Book?.Title ?? "Không rõ",
                Author = bd.Book?.BookAuthors.FirstOrDefault()?.Author?.AuthorName ?? "Không rõ",
                Category = bd.Book?.Category?.CategoryName ?? "Không phân loại",
                BorrowDate = bd.Borrow.BorrowDate,
                DueDate = bd.Borrow.DueDate,
                Quantity = bd.Quantity,
                IsOverdue = bd.Borrow.DueDate < DateTime.Now,
                DaysOverdue = bd.Borrow.DueDate < DateTime.Now 
                    ? (DateTime.Now - bd.Borrow.DueDate).Days 
                    : 0
            }).ToList();
        }

        // THÊM: Lấy lịch sử mượn của user
        private async Task<List<UserBorrowHistoryInfo>> GetUserBorrowHistory(int userId)
        {
            var borrows = await _context.Borrows
                .Include(b => b.BorrowDetails!)
                    .ThenInclude(bd => bd.Book)
                        .ThenInclude(book => book.Category)
                .Where(b => b.UserID == userId)
                .OrderByDescending(b => b.BorrowDate)
                .Take(20) // Lấy 20 phiếu mượn gần nhất
                .ToListAsync();
            
            var result = new List<UserBorrowHistoryInfo>();
            
            foreach (var borrow in borrows)
            {
                var bookList = new List<string>();
                int totalBooks = 0;
                
                if (borrow.BorrowDetails != null)
                {
                    foreach (var bd in borrow.BorrowDetails.Where(bd => bd.Book != null))
                    {
                        var title = bd.Book!.Title ?? "Không rõ";
                        bookList.Add($"{title} (x{bd.Quantity})");
                        totalBooks += bd.Quantity;
                    }
                }
                
                result.Add(new UserBorrowHistoryInfo
                {
                    BorrowID = borrow.BorrowID,
                    BorrowDate = borrow.BorrowDate,
                    DueDate = borrow.DueDate,
                    Status = borrow.Status,
                    Books = bookList,
                    TotalBooks = totalBooks,
                    IsReturned = borrow.Status == "Returned",
                    IsOverdue = borrow.Status != "Returned" && borrow.DueDate < DateTime.Now
                });
            }
            
            return result;
        }

        // Helper classes
        private class BookInfo
        {
            public int BookID { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Author { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public int Year { get; set; }
            public string Available { get; set; } = string.Empty;
        }

        private class BookDetailInfo
        {
            public int BookID { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Author { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public string Publisher { get; set; } = string.Empty;
            public int Year { get; set; }
            public int Quantity { get; set; }
            public string Available { get; set; } = string.Empty;
            public string CreatedAt { get; set; } = string.Empty;
        }

        private class CategoryInfo
        {
            public string Name { get; set; } = string.Empty;
            public int BookCount { get; set; }
            public string Description { get; set; } = string.Empty;
        }

        private class BorrowInfo
        {
            public string UserName { get; set; } = string.Empty;
            public DateTime DueDate { get; set; }
            public List<string> Books { get; set; } = new();
            public int DaysOverdue { get; set; }
        }

        private class AuthorInfo
        {
            public string Name { get; set; } = string.Empty;
            public int BookCount { get; set; }
            public string? LatestBook { get; set; }
        }

        private class AuthorDetailInfo
        {
            public int AuthorID { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public int BookCount { get; set; }
            public string Biography { get; set; } = string.Empty;
        }

        private class PublisherInfo
        {
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
        }

        private class PostInfo
        {
            public string Title { get; set; } = string.Empty;
            public string Author { get; set; } = string.Empty;
            public string MenuName { get; set; } = string.Empty;
            public string CreatedDate { get; set; } = string.Empty;
        }

        private class UserBorrowInfo
        {
            public int BookID { get; set; }
            public string BookTitle { get; set; } = string.Empty;
            public string Author { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public DateTime BorrowDate { get; set; }
            public DateTime DueDate { get; set; }
            public int Quantity { get; set; }
            public bool IsOverdue { get; set; }
            public int DaysOverdue { get; set; }
        }

        private class UserBorrowHistoryInfo
        {
            public int BorrowID { get; set; }
            public DateTime BorrowDate { get; set; }
            public DateTime DueDate { get; set; }
            public string Status { get; set; } = string.Empty;
            public List<string> Books { get; set; } = new();
            public int TotalBooks { get; set; }
            public bool IsReturned { get; set; }
            public bool IsOverdue { get; set; }
        }
    }
}