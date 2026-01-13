using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien.Models;
using QuanLyThuVien.Utilities;
using QuanLyThuVien.Services;
using PagedList.Core;

namespace QuanLyThuVien.Controllers
{
    public class BookController : Controller
    {
        private readonly DataContext _context;
        private readonly PdfAnalysisService? _pdfAnalysisService;
        private readonly TextToSpeechService? _ttsService;

        public BookController(DataContext context, PdfAnalysisService? pdfAnalysisService = null, TextToSpeechService? ttsService = null)
        {
            _context = context;
            _pdfAnalysisService = pdfAnalysisService;
            _ttsService = ttsService;
        }

        // GET: Book/Index - Trang sách & tài liệu với chức năng mượn
        [HttpGet]
        public IActionResult Index(int page = 1, string searchString = "", int? categoryId = null, int? publisherId = null, int? authorId = null, int? publishedYear = null)
        {
            int pageSize = 6;
            var books = _context.Books
                .Include(b => b.Category)
                .Include(b => b.Publisher)
                .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                .Where(b => b.IsActive == true)
                .AsQueryable();

            // Tìm kiếm theo tên sách
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                books = books.Where(b => b.Title != null && b.Title.Contains(searchString));
            }

            // Lọc theo danh mục
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                books = books.Where(b => b.CategoryID == categoryId.Value);
            }

            // Lọc theo nhà xuất bản
            if (publisherId.HasValue && publisherId.Value > 0)
            {
                books = books.Where(b => b.PublisherID == publisherId.Value);
            }

            // Lọc theo tác giả
            if (authorId.HasValue && authorId.Value > 0)
            {
                books = books.Where(b => b.BookAuthors != null && b.BookAuthors.Any(ba => ba.AuthorID == authorId.Value));
            }

            // Lọc theo năm xuất bản
            if (publishedYear.HasValue && publishedYear.Value > 0)
            {
                books = books.Where(b => b.PublishedYear == publishedYear.Value);
            }

            books = books.OrderByDescending(b => b.CreatedAt);
            var models = new PagedList<tblBook>(books, page, pageSize);

            // Lấy danh sách filter từ TẤT CẢ sách có sẵn (query trực tiếp từ database)
            var availableBookIds = _context.Books
                .Where(b => b.IsActive == true && b.Quantity > 0)
                .Select(b => b.CategoryID)
                .Distinct()
                .ToList();

            ViewBag.Categories = _context.Categories
                .Where(c => c.IsActive == true && availableBookIds.Contains(c.CategoryID))
                .OrderBy(c => c.CategoryName)
                .ToList();

            var availablePublisherIds = _context.Books
                .Where(b => b.IsActive == true && b.Quantity > 0)
                .Select(b => b.PublisherID)
                .Distinct()
                .ToList();

            ViewBag.Publishers = _context.Publishers
                .Where(p => p.IsActive == true && availablePublisherIds.Contains(p.PublisherID))
                .OrderBy(p => p.PublisherName)
                .ToList();

            var availableAuthorIds = _context.BookAuthors
                .Where(ba => ba.Book != null && ba.Book.IsActive == true && ba.Book.Quantity > 0)
                .Select(ba => ba.AuthorID)
                .Distinct()
                .ToList();

            ViewBag.Authors = _context.Authors
                .Where(a => a.IsActive == true && availableAuthorIds.Contains(a.AuthorID))
                .OrderBy(a => a.AuthorName)
                .ToList();

            ViewBag.PublishedYears = _context.Books
                .Where(b => b.IsActive == true && b.Quantity > 0)
                .Select(b => b.PublishedYear)
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();
            ViewBag.SearchString = searchString;
            ViewBag.CategoryId = categoryId;
            ViewBag.PublisherId = publisherId;
            ViewBag.AuthorId = authorId;
            ViewBag.PublishedYear = publishedYear;

            return View(models);
        }

        // POST: Book/Index - Xử lý mượn sách
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(IFormCollection form)
        {
            if (!Functions.IsLogin())
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để mượn sách!";
                return RedirectToAction("Login", "Account");
            }

            // KIỂM TRA: Nếu user có sách quá hạn chưa trả thì không cho mượn
            var overdueDetails = _context.BorrowDetails
                .Include(bd => bd.Borrow)
                .Where(bd => bd.Borrow.UserID == Functions._UserID
                    && bd.BorrowStatus != "Returned"
                    && bd.ReturnDate == null
                    && bd.Borrow.DueDate < DateTime.Now)
                .ToList();

            if (overdueDetails.Any())
            {
                var overdueCount = overdueDetails.Count;
                var overdueBorrowIds = overdueDetails.Select(bd => bd.Borrow.BorrowID).Distinct().ToList();
                TempData["ErrorMessage"] = $"Bạn có {overdueCount} cuốn sách quá hạn chưa trả (trong {overdueBorrowIds.Count} phiếu mượn). " +
                    $"Vui lòng trả hết sách quá hạn trước khi mượn sách mới!<br>" +
                    $"<a href=\"{Url.Action("History", "Borrow")}\" class=\"btn btn-sm btn-warning mt-2\">Xem lịch sử mượn</a>";
                return RedirectToAction("Index");
            }

            // Lấy danh sách sách đã chọn từ form (chỉ lấy các checkbox đã checked)
            var selectedBooks = new Dictionary<int, int>();
            var selectedBookIds = form["selectedBooks"].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);

            foreach (var bookIdStr in selectedBookIds)
            {
                if (int.TryParse(bookIdStr, out int bookId))
                {
                    var quantityKey = $"quantity_{bookId}";
                    if (form.ContainsKey(quantityKey) && int.TryParse(form[quantityKey], out int quantity) && quantity > 0)
                    {
                        selectedBooks[bookId] = quantity;
                    }
                }
            }

            if (selectedBooks.Count == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một cuốn sách để mượn!";
                return RedirectToAction("Index");
            }

            // Kiểm tra tồn kho và validate
            var errors = new List<string>();
            var borrowDetails = new List<tblBorrowDetail>();

            foreach (var item in selectedBooks)
            {
                var bookId = item.Key;
                var quantity = item.Value;

                if (quantity <= 0)
                    continue;

                var book = _context.Books.Find(bookId);
                if (book == null)
                {
                    errors.Add($"Sách ID {bookId} không tồn tại!");
                    continue;
                }

                if (book.Quantity < quantity)
                {
                    errors.Add($"Sách '{book.Title}' chỉ còn {book.Quantity} cuốn, không đủ số lượng bạn yêu cầu ({quantity} cuốn)!");
                    continue;
                }

                if (book.IsActive != true)
                {
                    errors.Add($"Sách '{book.Title}' hiện không khả dụng!");
                    continue;
                }

                // Tạo BorrowDetail (mỗi cuốn sách là 1 record)
                for (int i = 0; i < quantity; i++)
                {
                    borrowDetails.Add(new tblBorrowDetail
                    {
                        BookID = bookId,
                        BorrowStatus = "Borrowed",
                        Quantity = 1
                    });
                }
            }

            if (errors.Count > 0)
            {
                TempData["ErrorMessage"] = string.Join("<br>", errors);
                return RedirectToAction("Index");
            }

            if (borrowDetails.Count == 0)
            {
                TempData["ErrorMessage"] = "Không có sách nào hợp lệ để mượn!";
                return RedirectToAction("Index");
            }

            // Tạo phiếu mượn
            var borrow = new tblBorrow
            {
                UserID = Functions._UserID,
                BorrowDate = DateTime.Now,
                DueDate = DateTime.Now.AddDays(14), // Mượn 14 ngày
                Status = "Borrowing",
                BorrowDetails = borrowDetails
            };

            // Trừ số lượng sách
            foreach (var item in selectedBooks)
            {
                var bookId = item.Key;
                var quantity = item.Value;

                if (quantity <= 0)
                    continue;

                var book = _context.Books.Find(bookId);
                if (book != null)
                {
                    book.Quantity -= quantity;
                }
            }

            _context.Borrows.Add(borrow);
            _context.SaveChanges();

            TempData["SuccessMessage"] = $"Đã tạo phiếu mượn thành công! Mã phiếu: {borrow.BorrowID}. Bạn đã mượn {borrowDetails.Count} cuốn sách.";
            return RedirectToAction("Index");
        }

        // GET: Book/Details/{id} - Chi tiết sách cho user
        [HttpGet]
        public IActionResult Details(int id)
        {
            var book = _context.Books
                .Include(b => b.Category)
                .Include(b => b.Publisher)
                .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                .FirstOrDefault(b => b.BookID == id && b.IsActive == true);

            if (book == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sách!";
                return RedirectToAction("Index");
            }

            // AI-powered recommendation: Gợi ý sách liên quan
            var relatedBooks = GetRelatedBooks(book);
            ViewBag.RelatedBooks = relatedBooks;

            return View(book);
        }

        // AI Logic: Tìm sách liên quan dựa trên nhiều yếu tố
        private List<tblBook> GetRelatedBooks(tblBook currentBook)
        {
            var allBooks = _context.Books
                .Include(b => b.Category)
                .Include(b => b.Publisher)
                .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                .Where(b => b.IsActive == true && b.BookID != currentBook.BookID)
                .ToList();

            // Tính điểm tương đồng cho mỗi cuốn sách
            var scoredBooks = allBooks.Select(book => new
            {
                Book = book,
                Score = CalculateSimilarityScore(currentBook, book)
            })
            .Where(x => x.Score > 0) // Chỉ lấy sách có điểm > 0
            .OrderByDescending(x => x.Score)
            .Take(4) // Lấy 4 sách liên quan nhất
            .Select(x => x.Book)
            .ToList();

            return scoredBooks;
        }

        // Tính điểm tương đồng giữa 2 cuốn sách
        private int CalculateSimilarityScore(tblBook book1, tblBook book2)
        {
            int score = 0;

            // Cùng danh mục: +50 điểm
            if (book1.CategoryID == book2.CategoryID)
                score += 50;

            // Cùng nhà xuất bản: +20 điểm
            if (book1.PublisherID == book2.PublisherID)
                score += 20;

            // Cùng tác giả: +40 điểm
            if (book1.BookAuthors != null && book2.BookAuthors != null)
            {
                var authors1 = book1.BookAuthors.Select(ba => ba.AuthorID).ToList();
                var authors2 = book2.BookAuthors.Select(ba => ba.AuthorID).ToList();
                var commonAuthors = authors1.Intersect(authors2).Count();
                score += commonAuthors * 40;
            }

            // Năm xuất bản gần nhau: +10 điểm (trong vòng 3 năm)
            if (book1.PublishedYear > 0 && book2.PublishedYear > 0)
            {
                var yearDiff = Math.Abs(book1.PublishedYear - book2.PublishedYear);
                if (yearDiff <= 3)
                    score += (10 - (yearDiff * 2));
            }

            // Tiêu đề có từ chung: +5 điểm mỗi từ (tối đa 15)
            if (!string.IsNullOrEmpty(book1.Title) && !string.IsNullOrEmpty(book2.Title))
            {
                var words1 = book1.Title.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var words2 = book2.Title.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var commonWords = words1.Intersect(words2).Count();
                score += Math.Min(commonWords * 5, 15);
            }

            return score;
        }

        // ===== AI PDF ANALYSIS ACTIONS =====

        /// <summary>
        /// Analyze PDF content using AI
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AnalyzePdf(int bookId, string analysisType = "summary")
        {
            try
            {
                if (_pdfAnalysisService == null)
                {
                    return Json(new { success = false, message = "Dịch vụ phân tích PDF chưa được cấu hình" });
                }

                var book = await _context.Books
                    .Include(b => b.Category)
                    .FirstOrDefaultAsync(b => b.BookID == bookId);

                if (book == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sách" });
                }

                if (string.IsNullOrEmpty(book.BookFile))
                {
                    return Json(new { success = false, message = "Sách này không có file PDF" });
                }

                var result = await _pdfAnalysisService.AnalyzePdfContent(
                    book.BookFile, 
                    book.Title ?? "Sách", 
                    analysisType
                );

                return Json(new { success = true, result = result, analysisType = analysisType });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        /// <summary>
        /// Extract text from PDF (for preview/search)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ExtractPdfText([FromBody] PdfExtractRequest request)
        {
            try
            {
                if (_pdfAnalysisService == null)
                {
                    return Json(new { success = false, message = "Dịch vụ phân tích PDF chưa được cấu hình" });
                }

                var book = await _context.Books.FindAsync(request.BookId);

                if (book == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sách" });
                }

                if (string.IsNullOrEmpty(book.BookFile))
                {
                    return Json(new { success = false, message = "Sách này không có file PDF" });
                }

                var text = await _pdfAnalysisService.ExtractTextFromPdf(book.BookFile, request.MaxPages);

                return Json(new { success = true, text = text, maxPages = request.MaxPages });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        /// <summary>
        /// AI Text-to-Speech với phát hiện ngôn ngữ tự động
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> TextToSpeech([FromBody] TextToSpeechRequest request)
        {
            try
            {
                if (_ttsService == null)
                {
                    return Json(new { success = false, message = "Dịch vụ TTS chưa được cấu hình. Sử dụng Web Speech API.", useWebSpeech = true });
                }

                if (string.IsNullOrWhiteSpace(request.Text))
                {
                    return Json(new { success = false, message = "Không có văn bản để đọc" });
                }

                var (audioData, useWebSpeech, language) = await _ttsService.SynthesizeSpeechWithFallbackAsync(request.Text);

                if (useWebSpeech || audioData == null)
                {
                    return Json(new { success = true, useWebSpeech = true, language = language });
                }

                // Trả về audio file
                return File(audioData, "audio/mpeg", $"speech_{DateTime.Now:yyyyMMddHHmmss}.mp3");
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}", useWebSpeech = true });
            }
        }

        /// <summary>
        /// Request model for PDF extraction
        /// </summary>
        public class PdfExtractRequest
        {
            public int BookId { get; set; }
            public int MaxPages { get; set; } = 10;
        }

        /// <summary>
        /// Request model for Text-to-Speech
        /// </summary>
        public class TextToSpeechRequest
        {
            public string Text { get; set; } = "";
            public string? LanguageCode { get; set; }
        }

        /// <summary>
        /// Get quick summary of PDF (first 5 pages)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> GetQuickSummary(int bookId)
        {
            try
            {
                if (_pdfAnalysisService == null)
                {
                    return Json(new { success = false, message = "Dịch vụ phân tích PDF chưa được cấu hình" });
                }

                var book = await _context.Books.FindAsync(bookId);

                if (book == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sách" });
                }

                if (string.IsNullOrEmpty(book.BookFile))
                {
                    return Json(new { success = false, message = "Sách này không có file PDF" });
                }

                var summary = await _pdfAnalysisService.GetQuickSummary(book.BookFile, book.Title ?? "Sách");

                return Json(new { success = true, summary = summary });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }
    }
}
