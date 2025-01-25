using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Data;
using System.IO;
using book_management.Models;

namespace book_management.Pages
{
    public class ViewBooksModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        public List<Book> Books { get; set; } = new();

        public ViewBooksModel(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        public void OnGet()
        {
            LoadBooks();
        }

        private void LoadBooks()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    using SqlCommand cmd = new SqlCommand(
                        "SELECT * FROM books_inv", con);

                    using SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    Books = dt.AsEnumerable()
                        .Select(row =>
                        {
                            var filename = row["filename"].ToString();
                            var imageUrl = filename.StartsWith("~") 
                                ? filename.Replace("~", "") 
                                : $"~/images/{filename.TrimStart('@')}";

                            return new Book
                            {
                                BookId = row["book_id"].ToString(),
                                BookName = row["book_name"].ToString(),
                                Author = row["author"].ToString(),
                                Language = row["language"].ToString(),
                                Genre = row["genre"].ToString(),
                                CurrentStock = Convert.ToInt32(row["current_stock"]),
                                IssuedBooks = Convert.ToInt32(row["issued_books"]),
                                Description = row["book_description"].ToString(),
                                ImageUrl = imageUrl
                            };
                        }).ToList();
                }
            }
            catch (Exception ex)
            {
                // Silently handle error or log to proper logging system
            }
        }
    }
}