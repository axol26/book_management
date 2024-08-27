using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace book_management.Pages
{
    public class BookIssuingModel : PageModel
    {
        private readonly IConfiguration _configuration;
        public string ErrorMessage { get; set; }
        public string SuccessMessage { get; set; }

        public BookIssuingModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty]
        public string MemberId { get; set; }
        [BindProperty]
        public string BookId { get; set; }
        [BindProperty]
        public string BookName { get; set; }
        [BindProperty]
        public string MemberName { get; set; }
        [BindProperty]
        public string IssueDate { get; set; }
        [BindProperty]
        public string DueDate { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (HttpContext.Session.GetString("role") != "admin")
            {
                return RedirectToPage("/AdminLogin");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostGetMemberDetailsAsync()
        {
            if (string.IsNullOrEmpty(MemberId))
            {
                ErrorMessage = "Member ID is required.";
                return Page();
            }

            try
            {
                string connectionString = _configuration.GetConnectionString("con");
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await con.OpenAsync();
                    SqlCommand cmd = new SqlCommand("SELECT * FROM member WHERE member_id = @member_id", con);
                    cmd.Parameters.AddWithValue("@member_id", MemberId.Trim());

                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        MemberName = dt.Rows[0]["name"].ToString();
                    }
                    else
                    {
                        ErrorMessage = "Member does not exist.";
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostGetBookDetailsAsync()
        {
            if (string.IsNullOrEmpty(BookId))
            {
                ErrorMessage = "Book ID is required.";
                return Page();
            }

            try
            {
                string connectionString = _configuration.GetConnectionString("con");
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await con.OpenAsync();
                    SqlCommand cmd = new SqlCommand("SELECT * FROM books_inv WHERE book_id = @book_id", con);
                    cmd.Parameters.AddWithValue("@book_id", BookId.Trim());

                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        BookName = dt.Rows[0]["book_name"].ToString();
                    }
                    else
                    {
                        ErrorMessage = "Book does not exist.";
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostIssueBookAsync()
        {
            if (string.IsNullOrEmpty(MemberId) || string.IsNullOrEmpty(BookId) || string.IsNullOrEmpty(IssueDate) || string.IsNullOrEmpty(DueDate))
            {
                ErrorMessage = "All fields are required.";
                return Page();
            }

            if (await CheckBookAlreadyIssuedAsync())
            {
                ErrorMessage = "Book already issued to Member.";
                return Page();
            }

            try
            {
                string connectionString = _configuration.GetConnectionString("con");
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await con.OpenAsync();

                    // Update book stock and issued count
                    SqlCommand cmd = new SqlCommand("SELECT * FROM books_inv WHERE book_id = @book_id", con);
                    cmd.Parameters.AddWithValue("@book_id", BookId.Trim());

                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    int newIssue = int.Parse(dt.Rows[0]["issued_books"].ToString()) + 1;
                    int newStock = int.Parse(dt.Rows[0]["current_stock"].ToString()) - 1;

                    if (newStock < 0)
                    {
                        ErrorMessage = "No more books in stock.";
                        return Page();
                    }

                    cmd = new SqlCommand("UPDATE books_inv SET current_stock = @current_stock, issued_books = @issued_books WHERE book_id = @book_id", con);
                    cmd.Parameters.AddWithValue("@book_id", BookId.Trim());
                    cmd.Parameters.AddWithValue("@current_stock", newStock);
                    cmd.Parameters.AddWithValue("@issued_books", newIssue);

                    await cmd.ExecuteNonQueryAsync();

                    // Insert new book issue record
                    cmd = new SqlCommand("INSERT INTO books_issue (member_id, member_name, book_id, book_name, issue_date, due_date) VALUES (@member_id, @member_name, @book_id, @book_name, @issue_date, @due_date)", con);
                    cmd.Parameters.AddWithValue("@member_id", MemberId.Trim());
                    cmd.Parameters.AddWithValue("@member_name", MemberName.Trim());
                    cmd.Parameters.AddWithValue("@book_id", BookId.Trim());
                    cmd.Parameters.AddWithValue("@book_name", BookName.Trim());
                    cmd.Parameters.AddWithValue("@issue_date", IssueDate.Trim());
                    cmd.Parameters.AddWithValue("@due_date", DueDate.Trim());

                    await cmd.ExecuteNonQueryAsync();
                }

                SuccessMessage = "Book issued successfully.";
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostReturnBookAsync()
        {
            if (string.IsNullOrEmpty(MemberId) || string.IsNullOrEmpty(BookId))
            {
                ErrorMessage = "Member ID and Book ID are required.";
                return Page();
            }

            if (!await CheckBookAlreadyIssuedAsync())
            {
                ErrorMessage = "Book is not issued to Member.";
                return Page();
            }

            try
            {
                string connectionString = _configuration.GetConnectionString("con");
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await con.OpenAsync();

                    // Update book stock and issued count
                    SqlCommand cmd = new SqlCommand("SELECT * FROM books_inv WHERE book_id = @book_id", con);
                    cmd.Parameters.AddWithValue("@book_id", BookId.Trim());

                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    int newIssue = int.Parse(dt.Rows[0]["issued_books"].ToString()) - 1;
                    int newStock = int.Parse(dt.Rows[0]["current_stock"].ToString()) + 1;

                    if (newIssue < 0)
                    {
                        ErrorMessage = "Returning what???";
                        return Page();
                    }

                    cmd = new SqlCommand("UPDATE books_inv SET current_stock = @current_stock, issued_books = @issued_books WHERE book_id = @book_id", con);
                    cmd.Parameters.AddWithValue("@book_id", BookId.Trim());
                    cmd.Parameters.AddWithValue("@current_stock", newStock);
                    cmd.Parameters.AddWithValue("@issued_books", newIssue);

                    await cmd.ExecuteNonQueryAsync();

                    // Delete book issue record
                    cmd = new SqlCommand("DELETE FROM books_issue WHERE member_id = @member_id AND book_id = @book_id", con);
                    cmd.Parameters.AddWithValue("@member_id", MemberId.Trim());
                    cmd.Parameters.AddWithValue("@book_id", BookId.Trim());

                    await cmd.ExecuteNonQueryAsync();
                }

                SuccessMessage = "Book returned successfully.";
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            return RedirectToPage();
        }

        private async Task<bool> CheckBookAlreadyIssuedAsync()
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("con");
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await con.OpenAsync();
                    SqlCommand cmd = new SqlCommand("SELECT * FROM books_issue WHERE member_id = @member_id AND book_id = @book_id", con);
                    cmd.Parameters.AddWithValue("@book_id", BookId.Trim());
                    cmd.Parameters.AddWithValue("@member_id", MemberId.Trim());

                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    return dt.Rows.Count > 0;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }
    }
}
