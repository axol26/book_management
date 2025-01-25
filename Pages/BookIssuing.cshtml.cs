using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Data;

namespace book_management.Pages
{
    public class BookIssuingModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        [BindProperty]
        public string MemberId { get; set; }
        [BindProperty]
        public string BookId { get; set; }
        [BindProperty]
        public string MemberName { get; set; }
        [BindProperty]
        public string BookName { get; set; }
        [BindProperty]
        public DateTime IssueDate { get; set; } = DateTime.Today;
        [BindProperty]
        public DateTime DueDate { get; set; } = DateTime.Today.AddDays(14);

        public List<BookIssue> IssuedBooks { get; set; } = new();

        public BookIssuingModel(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        public IActionResult OnGet()
        {
            if (HttpContext.Session.GetString("role") != "admin")
            {
                return RedirectToPage("/AdminLogin");
            }

            LoadIssuedBooks();
            return Page();
        }

        public async Task<IActionResult> OnPostSearchAsync()
        {
            if (HttpContext.Session.GetString("role") != "admin")
            {
                return RedirectToPage("/AdminLogin");
            }

            if (!await CheckIfMemberExists())
            {
                TempData["Message"] = "Member does not exist.";
            }
            else if (!await CheckBookExists())
            {
                TempData["Message"] = "Book does not exist.";
            }
            else
            {
                await GetDetails();
            }

            LoadIssuedBooks();
            return Page();
        }

        public async Task<IActionResult> OnPostIssueAsync()
        {
            if (HttpContext.Session.GetString("role") != "admin")
            {
                return RedirectToPage("/AdminLogin");
            }

            if (string.IsNullOrEmpty(MemberName) || string.IsNullOrEmpty(BookName))
            {
                TempData["Message"] = "Generate Member ID and Book ID.";
            }
            else if (await CheckBookAlreadyIssued())
            {
                TempData["Message"] = "Book already issued to Member.";
            }
            else
            {
                await IssueBook();
            }

            LoadIssuedBooks();
            return Page();
        }

        public async Task<IActionResult> OnPostReturnAsync()
        {
            if (HttpContext.Session.GetString("role") != "admin")
            {
                return RedirectToPage("/AdminLogin");
            }

            if (!await CheckBookAlreadyIssued())
            {
                TempData["Message"] = "Book is not issued to Member.";
            }
            else
            {
                await ReturnBook();
            }

            LoadIssuedBooks();
            return Page();
        }

        private async Task<bool> CheckIfMemberExists()
        {
            try
            {
                using SqlConnection con = new(_connectionString);
                await con.OpenAsync();
                using SqlCommand cmd = new("SELECT * FROM member WHERE member_id = @member_id;", con);
                cmd.Parameters.AddWithValue("@member_id", MemberId?.Trim());

                using SqlDataAdapter da = new(cmd);
                DataTable dt = new();
                da.Fill(dt);
                return dt.Rows.Count > 0;
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Error checking member: {ex.Message}";
                return false;
            }
        }

        private async Task<bool> CheckBookExists()
        {
            try
            {
                using SqlConnection con = new(_connectionString);
                await con.OpenAsync();
                using SqlCommand cmd = new("SELECT * FROM books_inv WHERE book_id=@book_id", con);
                cmd.Parameters.AddWithValue("@book_id", BookId?.Trim());

                using SqlDataAdapter da = new(cmd);
                DataTable dt = new();
                da.Fill(dt);
                return dt.Rows.Count > 0;
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Error checking book: {ex.Message}";
                return false;
            }
        }

        private async Task GetDetails()
        {
            try
            {
                using SqlConnection con = new(_connectionString);
                await con.OpenAsync();

                // First command for member details
                using (var memberCmd = new SqlCommand("SELECT * FROM member WHERE member_id = @member_id;", con))
                {
                    memberCmd.Parameters.AddWithValue("@member_id", MemberId?.Trim());

                    using SqlDataAdapter da = new(memberCmd);
                    DataTable dt = new();
                    da.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        MemberName = dt.Rows[0]["name"].ToString();
                    }
                }

                // Second command for book details
                using (var bookCmd = new SqlCommand("SELECT * FROM books_inv WHERE book_id=@book_id;", con))
                {
                    bookCmd.Parameters.AddWithValue("@book_id", BookId?.Trim());

                    using SqlDataAdapter da = new(bookCmd);
                    DataTable dt = new();
                    da.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        BookName = dt.Rows[0]["book_name"].ToString();
                    }
                }

                // Check if book is already issued to this member
                using (var issueCmd = new SqlCommand("SELECT * FROM books_issue WHERE member_id=@member_id AND book_id=@book_id", con))
                {
                    issueCmd.Parameters.AddWithValue("@member_id", MemberId?.Trim());
                    issueCmd.Parameters.AddWithValue("@book_id", BookId?.Trim());

                    using SqlDataAdapter da = new(issueCmd);
                    DataTable dt = new();
                    da.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        // If book is issued, set the dates from the record
                        IssueDate = Convert.ToDateTime(dt.Rows[0]["issue_date"]);
                        DueDate = Convert.ToDateTime(dt.Rows[0]["due_date"]);
                    }
                    else
                    {
                        // If book is not issued, set default dates
                        IssueDate = DateTime.Today;
                        DueDate = DateTime.Today.AddDays(14);
                    }
                }

                if (string.IsNullOrEmpty(MemberName) || string.IsNullOrEmpty(BookName))
                {
                    TempData["Message"] = "Member or Book not found.";
                }
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Error getting details: {ex.Message}";
            }
        }

        private async Task<bool> CheckBookAlreadyIssued()
        {
            try
            {
                using SqlConnection con = new(_connectionString);
                await con.OpenAsync();
                using SqlCommand cmd = new("SELECT * FROM books_issue WHERE member_id=@member_id AND book_id=@book_id", con);
                cmd.Parameters.AddWithValue("@book_id", BookId?.Trim());
                cmd.Parameters.AddWithValue("@member_id", MemberId?.Trim());

                using SqlDataAdapter da = new(cmd);
                DataTable dt = new();
                da.Fill(dt);
                return dt.Rows.Count > 0;
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Error checking issued book: {ex.Message}";
                return false;
            }
        }

        private async Task IssueBook()
        {
            try
            {
                using SqlConnection con = new(_connectionString);
                await con.OpenAsync();

                // First get book details
                int newIssue, newStock;
                using (var checkCmd = new SqlCommand("SELECT * FROM books_inv WHERE book_id=@book_id", con))
                {
                    checkCmd.Parameters.AddWithValue("@book_id", BookId?.Trim());

                    using SqlDataAdapter da = new(checkCmd);
                    DataTable dt = new();
                    da.Fill(dt);

                    if (dt.Rows.Count == 0)
                    {
                        TempData["Message"] = "Book not found.";
                        return;
                    }

                    newIssue = Convert.ToInt32(dt.Rows[0]["issued_books"]) + 1;
                    newStock = Convert.ToInt32(dt.Rows[0]["current_stock"]) - 1;
                }

                // Then update book inventory
                using (var updateCmd = new SqlCommand("UPDATE books_inv SET current_stock=@current_stock, issued_books=@issued_books WHERE book_id=@book_id", con))
                {
                    updateCmd.Parameters.AddWithValue("@book_id", BookId?.Trim());
                    updateCmd.Parameters.AddWithValue("@current_stock", newStock);
                    updateCmd.Parameters.AddWithValue("@issued_books", newIssue);
                    await updateCmd.ExecuteNonQueryAsync();
                }

                // Finally insert issue record
                using (var insertCmd = new SqlCommand(@"INSERT INTO books_issue 
                    (member_id, member_name, book_id, book_name, issue_date, due_date) 
                    VALUES (@member_id, @member_name, @book_id, @book_name, @issue_date, @due_date)", con))
                {
                    insertCmd.Parameters.AddWithValue("@member_id", MemberId?.Trim() ?? "");
                    insertCmd.Parameters.AddWithValue("@member_name", MemberName?.Trim() ?? "");
                    insertCmd.Parameters.AddWithValue("@book_id", BookId?.Trim() ?? "");
                    insertCmd.Parameters.AddWithValue("@book_name", BookName?.Trim() ?? "");
                    insertCmd.Parameters.AddWithValue("@issue_date", IssueDate);
                    insertCmd.Parameters.AddWithValue("@due_date", DueDate);
                    await insertCmd.ExecuteNonQueryAsync();
                }

                TempData["Message"] = "Book issued successfully.";
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Error issuing book: {ex.Message}";
            }
        }

        private async Task ReturnBook()
        {
            try
            {
                using SqlConnection con = new(_connectionString);
                await con.OpenAsync();

                int newIssue, newStock;
                // First get book details
                using (var checkCmd = new SqlCommand("SELECT * FROM books_inv WHERE book_id=@book_id", con))
                {
                    checkCmd.Parameters.AddWithValue("@book_id", BookId?.Trim());

                    using SqlDataAdapter da = new(checkCmd);
                    DataTable dt = new();
                    da.Fill(dt);

                    if (dt.Rows.Count == 0)
                    {
                        TempData["Message"] = "Book not found.";
                        return;
                    }

                    newIssue = Convert.ToInt32(dt.Rows[0]["issued_books"]) - 1;
                    newStock = Convert.ToInt32(dt.Rows[0]["current_stock"]) + 1;

                    if (newIssue < 0)
                    {
                        TempData["Message"] = "Invalid return operation.";
                        return;
                    }
                }

                // Update book inventory
                using (var updateCmd = new SqlCommand("UPDATE books_inv SET current_stock=@current_stock, issued_books=@issued_books WHERE book_id=@book_id", con))
                {
                    updateCmd.Parameters.AddWithValue("@book_id", BookId?.Trim());
                    updateCmd.Parameters.AddWithValue("@current_stock", newStock);
                    updateCmd.Parameters.AddWithValue("@issued_books", newIssue);
                    await updateCmd.ExecuteNonQueryAsync();
                }

                // Delete issue record
                using (var deleteCmd = new SqlCommand("DELETE FROM books_issue WHERE member_id=@member_id AND book_id=@book_id", con))
                {
                    deleteCmd.Parameters.AddWithValue("@member_id", MemberId?.Trim());
                    deleteCmd.Parameters.AddWithValue("@book_id", BookId?.Trim());
                    await deleteCmd.ExecuteNonQueryAsync();
                }

                TempData["Message"] = "Book returned successfully.";
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Error returning book: {ex.Message}";
            }
        }

        private void LoadIssuedBooks()
        {
            try
            {
                using SqlConnection con = new(_connectionString);
                con.Open();
                using SqlCommand cmd = new("SELECT * FROM books_issue", con);
                using SqlDataAdapter da = new(cmd);
                DataTable dt = new();
                da.Fill(dt);

                IssuedBooks = dt.AsEnumerable()
                    .Select(row => new BookIssue
                    {
                        MemberId = row["member_id"].ToString(),
                        MemberName = row["member_name"].ToString(),
                        BookId = row["book_id"].ToString(),
                        BookName = row["book_name"].ToString(),
                        IssueDate = Convert.ToDateTime(row["issue_date"]),
                        DueDate = Convert.ToDateTime(row["due_date"])
                    }).ToList();
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Error loading issued books: {ex.Message}";
            }
        }
    }

    public class BookIssue
    {
        public string MemberId { get; set; }
        public string MemberName { get; set; }
        public string BookId { get; set; }
        public string BookName { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime DueDate { get; set; }
    }
}
