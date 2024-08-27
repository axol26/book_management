using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;

namespace book_management.Pages
{
    public class BookInventoryModel : PageModel
    {
        private readonly IConfiguration _configuration;
        public string ErrorMessage { get; set; }
        public string SuccessMessage { get; set; }

        public BookInventoryModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty]
        public string BookId { get; set; }
        [BindProperty]
        public string BookName { get; set; }
        [BindProperty]
        public string Language { get; set; }
        [BindProperty]
        public string Author { get; set; }
        [BindProperty]
        public int CurrentStock { get; set; }
        [BindProperty]
        public int IssuedBooks { get; set; }
        [BindProperty]
        public string BookDescription { get; set; }
        [BindProperty]
        public List<string> SelectedGenres { get; set; }
        [BindProperty]
        public IFormFile FileUpload { get; set; }
        
        public async Task<IActionResult> OnGetAsync()
        {
            if (HttpContext.Session.GetString("role") != "admin")
            {
                return RedirectToPage("/AdminLogin");
            }

            // Load any necessary data for the page
            // await LoadGenres();
            return Page();
        }

        public async Task<IActionResult> OnPostGetBookDetailsAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                string connectionString = _configuration.GetConnectionString("con");
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await con.OpenAsync();
                    SqlCommand cmd = new SqlCommand("SELECT * FROM books_inv WHERE book_id=@book_id", con);
                    cmd.Parameters.AddWithValue("@book_id", BookId.Trim());

                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        BookName = dt.Rows[0]["book_name"].ToString();
                        Language = dt.Rows[0]["language"].ToString().Trim();
                        CurrentStock = int.Parse(dt.Rows[0]["current_stock"].ToString());
                        Author = dt.Rows[0]["author"].ToString();
                        IssuedBooks = int.Parse(dt.Rows[0]["issued_books"].ToString());
                        BookDescription = dt.Rows[0]["book_description"].ToString();

                        // Set selected genres and other properties
                        // SelectedGenres = dt.Rows[0]["genre"].ToString().Split(',').ToList();
                        // global_filepath = dt.Rows[0]["filename"].ToString();
                    }
                    else
                    {
                        ErrorMessage = "Book ID does not exist.";
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAddBookAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                string connectionString = _configuration.GetConnectionString("con");
                string genres = string.Join(", ", SelectedGenres);
                string filepath = "~/images/default.png";
                
                if (FileUpload != null)
                {
                    string filename = Path.GetFileName(FileUpload.FileName);
                    string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", filename);
                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await FileUpload.CopyToAsync(stream);
                    }
                    filepath = $"~/images/{filename}";
                }

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await con.OpenAsync();
                    SqlCommand cmd = new SqlCommand("INSERT INTO books_inv (book_id, book_name, language, author, current_stock, issued_books, genre, book_description, filename) VALUES (@book_id, @book_name, @language, @author, @current_stock, @issued_books, @genre, @book_description, @filename)", con);
                    cmd.Parameters.AddWithValue("@book_id", BookId.Trim());
                    cmd.Parameters.AddWithValue("@book_name", BookName.Trim());
                    cmd.Parameters.AddWithValue("@language", Language);
                    cmd.Parameters.AddWithValue("@author", Author.Trim());
                    cmd.Parameters.AddWithValue("@current_stock", CurrentStock);
                    cmd.Parameters.AddWithValue("@issued_books", IssuedBooks);
                    cmd.Parameters.AddWithValue("@genre", genres);
                    cmd.Parameters.AddWithValue("@book_description", BookDescription.Trim());
                    cmd.Parameters.AddWithValue("@filename", filepath);

                    await cmd.ExecuteNonQueryAsync();
                }

                SuccessMessage = "Book added successfully.";
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateBookAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                string connectionString = _configuration.GetConnectionString("con");
                string genres = string.Join(", ", SelectedGenres);
                string filepath = "~/images/default.png";
                
                if (FileUpload != null)
                {
                    string filename = Path.GetFileName(FileUpload.FileName);
                    string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", filename);
                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await FileUpload.CopyToAsync(stream);
                    }
                    filepath = $"~/images/{filename}";
                }

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await con.OpenAsync();
                    SqlCommand cmd = new SqlCommand("UPDATE books_inv SET book_name=@book_name, language=@language, author=@author, genre=@genre, book_description=@book_description, filename=@filename WHERE book_id=@book_id", con);
                    cmd.Parameters.AddWithValue("@book_id", BookId.Trim());
                    cmd.Parameters.AddWithValue("@book_name", BookName.Trim());
                    cmd.Parameters.AddWithValue("@language", Language);
                    cmd.Parameters.AddWithValue("@author", Author.Trim());
                    cmd.Parameters.AddWithValue("@genre", genres);
                    cmd.Parameters.AddWithValue("@book_description", BookDescription.Trim());
                    cmd.Parameters.AddWithValue("@filename", filepath);

                    await cmd.ExecuteNonQueryAsync();
                }

                SuccessMessage = "Book updated successfully.";
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteBookAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                string connectionString = _configuration.GetConnectionString("con");

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await con.OpenAsync();
                    SqlCommand cmd = new SqlCommand("DELETE FROM books_inv WHERE book_id=@book_id", con);
                    cmd.Parameters.AddWithValue("@book_id", BookId.Trim());

                    await cmd.ExecuteNonQueryAsync();
                }

                SuccessMessage = "Book deleted successfully.";
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            return RedirectToPage();
        }
    }
}
