using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace book_management.Pages
{
    public class ProfileModel : PageModel
    {
        private readonly string _connectionString;
        private readonly IConfiguration _configuration;

        [BindProperty]
        public string FullName { get; set; }

        [BindProperty]
        public DateTime DateOfBirth { get; set; }

        [BindProperty]
        public string ContactNumber { get; set; }

        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        public string Country { get; set; }

        [BindProperty]
        public string MemberId { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public List<IssuedBook> IssuedBooks { get; set; } = new();

        public ProfileModel(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        public IActionResult OnGet()
        {
            try
            {
                var username = HttpContext.Session.GetString("username");
                var role = HttpContext.Session.GetString("role");

                if (string.IsNullOrEmpty(role))
                {
                    return RedirectToPage("/Login");
                }

                GetUserProfile();
                return Page();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return Page();
            }
        }

        public async Task<IActionResult> OnPostUpdate()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    await con.OpenAsync();

                    using SqlCommand cmd = new SqlCommand(
                        @"UPDATE member 
                        SET name=@name, 
                            dob=@dob, 
                            contact=@contact, 
                            email=@email, 
                            province=@province, 
                            password=@password 
                        WHERE member_id=@member_id", con);

                    cmd.Parameters.AddWithValue("@name", FullName ?? string.Empty);
                    cmd.Parameters.AddWithValue("@dob", DateOfBirth);
                    cmd.Parameters.AddWithValue("@contact", ContactNumber ?? string.Empty);
                    cmd.Parameters.AddWithValue("@email", Email ?? string.Empty);
                    cmd.Parameters.AddWithValue("@province", Country ?? string.Empty);
                    cmd.Parameters.AddWithValue("@password", Password ?? string.Empty);
                    cmd.Parameters.AddWithValue("@member_id", MemberId ?? string.Empty);

                    await cmd.ExecuteNonQueryAsync();
                }

                TempData["SuccessMessage"] = "Profile updated successfully!";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating profile: {ex.Message}";
                return Page();
            }
        }

        public IActionResult OnPostDelete()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    if (con.State == ConnectionState.Closed)
                    {
                        con.Open();
                    }

                    using SqlCommand cmd = new SqlCommand(
                        "DELETE FROM member WHERE member_id=@member_id", con);
                    
                    cmd.Parameters.AddWithValue("@member_id", MemberId);
                    cmd.ExecuteNonQuery();
                }

                HttpContext.Session.Clear();
                return RedirectToPage("/Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return Page();
            }
        }

        private void GetUserProfile()
        {
            using SqlConnection con = new SqlConnection(_connectionString);
            if (con.State == ConnectionState.Closed)
            {
                con.Open();
            }

            // First, let's verify what we're getting from the database
            using SqlCommand cmd = new SqlCommand(
                @"SELECT name, dob, contact, email, member_id, 
                password as member_password, province 
                FROM member 
                WHERE member_id = @member_id;", con);
            
            var username = HttpContext.Session.GetString("username");
            cmd.Parameters.AddWithValue("@member_id", username);

            using SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            da.Fill(dt);

            if (dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                FullName = row["name"]?.ToString();
                DateOfBirth = row["dob"] != DBNull.Value ? Convert.ToDateTime(row["dob"]) : DateTime.Now;
                ContactNumber = row["contact"]?.ToString();
                Email = row["email"]?.ToString();
                MemberId = row["member_id"]?.ToString();
                Password = row["member_password"]?.ToString(); // Changed to match the alias
                Country = row["province"]?.ToString()?.Trim();

                // Add debug information to TempData
                if (string.IsNullOrEmpty(Password))
                {
                    TempData["ErrorMessage"] = $"Debug: Password field is empty for user {username}. Raw value: {row["member_password"]}";
                }
            }
            else
            {
                TempData["ErrorMessage"] = $"Debug: No user found with ID {username}";
            }
        }
    }

    public class IssuedBook
    {
        public string BookId { get; set; }
        public string BookName { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime DueDate { get; set; }
    }
}

