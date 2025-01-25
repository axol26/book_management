using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Data;
using book_management.Models;

namespace book_management.Pages
{
    public class BookInventoryModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly IWebHostEnvironment _environment;
        private static string _globalFilepath;

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
        public string[] SelectedGenres { get; set; }
        [BindProperty]
        public string Description { get; set; }
        [BindProperty]
        public IFormFile FileUpload { get; set; }

        public List<string> GenreList { get; } = new()
        {
            "Action", "Adventure", "Comic Book", "Self Help", "Motivation",
            "Healthy Living", "Wellness", "Crime", "Drama", "Fantasy",
            "Horror", "Poetry", "Personal Development", "Romance",
            "Science Fiction", "Suspense", "Thriller", "Art",
            "Autobiography", "Encyclopedia", "Health", "History",
            "Math", "Textbook", "Science", "Travel"
        };

        public List<Book> Books { get; set; } = new();

        public BookInventoryModel(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
            _environment = environment;
        }

        public IActionResult OnGet()
        {
            if (HttpContext.Session.GetString("role") != "admin")
            {
                return RedirectToPage("/AdminLogin");
            }

            LoadBooks();
            return Page();
        }

        public async Task<IActionResult> OnPostSearchAsync()
        {
            if (HttpContext.Session.GetString("role") != "admin")
            {
                return RedirectToPage("/AdminLogin");
            }

            if (string.IsNullOrEmpty(BookId))
            {
                TempData["Message"] = "Please enter a Book ID";
                return Page();
            }

            await GetBookDetails();
            LoadBooks();

            // Keep the form values after postback
            ModelState.Clear(); // Clear validation errors
            return Page();
        }

        public async Task<IActionResult> OnPostAddAsync()
        {
            if (HttpContext.Session.GetString("role") != "admin")
            {
                return RedirectToPage("/AdminLogin");
            }

            if (await CheckBookExists())
            {
                TempData["Message"] = "Book ID already exists.";
                return RedirectToPage();
            }

            await AddBookDetails();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            if (HttpContext.Session.GetString("role") != "admin")
            {
                return RedirectToPage("/AdminLogin");
            }

            if (!await CheckBookExists())
            {
                TempData["Message"] = "Book ID does not exist.";
                return RedirectToPage();
            }

            await UpdateBookDetails();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            if (HttpContext.Session.GetString("role") != "admin")
            {
                return RedirectToPage("/AdminLogin");
            }

            if (!await CheckBookExists())
            {
                TempData["Message"] = "Book ID does not exist.";
                return RedirectToPage();
            }

            await DeleteBookDetails();
            return RedirectToPage();
        }

        private async Task GetBookDetails()
        {
            try
            {
                using SqlConnection con = new(_connectionString);
                if (con.State == ConnectionState.Closed)
                {
                    await con.OpenAsync();
                }

                using SqlCommand cmd = new("SELECT * FROM books_inv WHERE book_id=@book_id", con);
                cmd.Parameters.AddWithValue("@book_id", BookId?.Trim());

                using SqlDataAdapter da = new(cmd);
                DataTable dt = new();
                da.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    BookName = row["book_name"].ToString();
                    Language = row["language"].ToString().Trim();
                    CurrentStock = Convert.ToInt32(row["current_stock"]);
                    Author = row["author"].ToString();
                    IssuedBooks = Convert.ToInt32(row["issued_books"]);
                    Description = row["book_description"].ToString();

                    string[] genres = row["genre"].ToString().Split(',');
                    SelectedGenres = genres.Select(g => g.Trim()).ToArray();

                    _globalFilepath = row["filename"].ToString();

                    // Update the preview image
                    var filename = row["filename"].ToString();
                    var imageUrl = filename.StartsWith("~") 
                        ? filename.Replace("~", "") 
                        : $"~/images/{filename.TrimStart('@')}";
                    
                    TempData["PreviewImage"] = imageUrl;
                }
                else
                {
                    TempData["Message"] = "Book not found.";
                    ClearForm();
                }
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Error getting book details: {ex.Message}";
                ClearForm();
            }
        }

        private void ClearForm()
        {
            BookName = "";
            Language = "";
            CurrentStock = 0;
            Author = "";
            IssuedBooks = 0;
            Description = "";
            SelectedGenres = Array.Empty<string>();
            _globalFilepath = "";
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

        private async Task AddBookDetails()
        {
            try
            {
                // Very conservative limits
                string bookName = (BookName?.Trim() ?? "").Length > 30 ? BookName.Substring(0, 30) : BookName?.Trim();
                string author = (Author?.Trim() ?? "").Length > 30 ? Author.Substring(0, 30) : Author?.Trim();
                string description = (Description?.Trim() ?? "").Length > 100 ? Description.Substring(0, 100) : Description?.Trim();
                
                // Take only first genre
                string genres = SelectedGenres?.FirstOrDefault() ?? "";
                if (genres.Length > 30) genres = genres.Substring(0, 30);

                // Simple filename
                string filepath = "/images/books1.png";  // Default image path

                if (FileUpload != null && FileUpload.Length > 0)
                {
                    string extension = Path.GetExtension(FileUpload.FileName).ToLower();
                    string uniqueFileName = $"{Guid.NewGuid().ToString().Substring(0, 8)}{extension}";
                    filepath = $"/images/{uniqueFileName}";
                    
                    // Ensure directory exists
                    string uploadsFolder = Path.Combine(_environment.WebRootPath, "images");
                    Directory.CreateDirectory(uploadsFolder);
                    
                    // Save file
                    using (var fileStream = new FileStream(Path.Combine(uploadsFolder, uniqueFileName), FileMode.Create))
                    {
                        await FileUpload.CopyToAsync(fileStream);
                    }
                }

                using SqlConnection con = new(_connectionString);
                await con.OpenAsync();
                using SqlCommand cmd = new(@"
                    INSERT INTO books_inv (
                        book_id, 
                        book_name, 
                        language, 
                        author, 
                        current_stock, 
                        issued_books, 
                        genre, 
                        book_description, 
                        filename
                    ) VALUES (
                        @book_id,
                        @book_name,
                        @language,
                        @author,
                        10,
                        0,
                        @genre,
                        @book_description,
                        @filename
                    )", con);

                cmd.Parameters.AddWithValue("@book_id", (BookId?.Trim() ?? "").Length > 20 ? BookId.Substring(0, 20) : BookId?.Trim());
                cmd.Parameters.AddWithValue("@book_name", bookName);
                cmd.Parameters.AddWithValue("@language", Language);
                cmd.Parameters.AddWithValue("@author", author);
                cmd.Parameters.AddWithValue("@genre", genres);
                cmd.Parameters.AddWithValue("@book_description", description);
                cmd.Parameters.AddWithValue("@filename", filepath);

                await cmd.ExecuteNonQueryAsync();
                TempData["Message"] = "Book added successfully.";
                
                // Clear form after successful add
                ClearForm();
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Error adding book: {ex.Message}";
            }
        }

        private async Task UpdateBookDetails()
        {
            try
            {
                string genres = string.Join(", ", SelectedGenres);
                string filepath = FileUpload != null ? await SaveUploadedFile() : _globalFilepath;

                using SqlConnection con = new(_connectionString);
                await con.OpenAsync();
                using SqlCommand cmd = new(@"
                    UPDATE books_inv SET 
                    book_name=@book_name, language=@language, author=@author, 
                    genre=@genre, book_description=@book_description, filename=@filename 
                    WHERE book_id=@book_id", con);

                cmd.Parameters.AddWithValue("@book_id", BookId?.Trim());
                cmd.Parameters.AddWithValue("@book_name", BookName?.Trim());
                cmd.Parameters.AddWithValue("@language", Language);
                cmd.Parameters.AddWithValue("@author", Author?.Trim());
                cmd.Parameters.AddWithValue("@genre", genres);
                cmd.Parameters.AddWithValue("@book_description", Description?.Trim());
                cmd.Parameters.AddWithValue("@filename", filepath);

                await cmd.ExecuteNonQueryAsync();
                TempData["Message"] = "Book updated successfully.";
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Error updating book: {ex.Message}";
            }
        }

        private async Task DeleteBookDetails()
        {
            try
            {
                using SqlConnection con = new(_connectionString);
                await con.OpenAsync();
                using SqlCommand cmd = new("DELETE FROM books_inv WHERE book_id=@book_id", con);
                cmd.Parameters.AddWithValue("@book_id", BookId?.Trim());

                await cmd.ExecuteNonQueryAsync();
                TempData["Message"] = "Book deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Error deleting book: {ex.Message}";
            }
        }

        private async Task<string> SaveUploadedFile()
        {
            if (FileUpload == null || FileUpload.Length == 0)
            {
                return "~/images/books1.png";
            }

            string uploadsFolder = Path.Combine(_environment.WebRootPath, "images");
            string uniqueFileName = $"{Guid.NewGuid()}_{FileUpload.FileName}";
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await FileUpload.CopyToAsync(fileStream);
            }

            return $"~/images/{uniqueFileName}";
        }

        private void LoadBooks()
        {
            try
            {
                using SqlConnection con = new(_connectionString);
                con.Open();
                using SqlCommand cmd = new("SELECT * FROM books_inv", con);
                using SqlDataAdapter da = new(cmd);
                DataTable dt = new();
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
            catch (Exception ex)
            {
                TempData["Message"] = $"Error loading books: {ex.Message}";
            }
        }
    }
}
