using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Data;

namespace book_management.Pages
{
    public class SignUpModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly ILogger<SignUpModel> _logger;

        public SignUpModel(IConfiguration configuration, ILogger<SignUpModel> logger)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
        } 

        [BindProperty]
        public string FullName { get; set; }
        
        [BindProperty]
        public DateTime DateOfBirth { get; set; }
        
        [BindProperty]
        public string ContactNumber { get; set; }
        
        [BindProperty]
        public string EmailId { get; set; }

        [BindProperty]
        public string Province { get; set; }
        
        [BindProperty]
        public string Country { get; set; }
        
        [BindProperty]
        public string MemberId { get; set; }
        
        [BindProperty]
        public string Password { get; set; }

        public void OnGet()
        {
            // Initialization logic if needed
        }

        public IActionResult OnPostSignUp()
        {
            try
            {
                if (CheckIfMemberExists())
                {
                    TempData["AlertMessage"] = "Member ID already exists. Please choose a different one.";
                    return Page();
                }

                SignUpUser();
                
                // If we got here and there's no error message, signup was successful
                if (TempData["AlertMessage"]?.ToString() == "Member Sign Up Successful, Please Login.")
                {
                    return RedirectToPage("/Login");
                }

                // If there was an error, it will be in TempData["AlertMessage"]
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during sign up");
                TempData["AlertMessage"] = "An unexpected error occurred during sign up.";
                return Page();
            }
        }

        private bool CheckIfMemberExists()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("SELECT * FROM member WHERE member_id = @member_id;", con);
                    cmd.Parameters.AddWithValue("@member_id", MemberId);

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        return dt.Rows.Count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["AlertMessage"] = ex.Message;
                return false;
            }
        }

        private void SignUpUser()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand(
                        "INSERT INTO member (name, dob, contact, email, province, member_id, password) " +
                        "VALUES (@name, @dob, @contact, @email, @province, @member_id, @password)", con);

                    cmd.Parameters.AddWithValue("@name", FullName);
                    cmd.Parameters.AddWithValue("@dob", DateOfBirth);
                    cmd.Parameters.AddWithValue("@contact", ContactNumber);
                    cmd.Parameters.AddWithValue("@email", EmailId);
                    cmd.Parameters.AddWithValue("@province", Country);
                    cmd.Parameters.AddWithValue("@member_id", MemberId);
                    cmd.Parameters.AddWithValue("@password", Password);

                    cmd.ExecuteNonQuery();
                }
                TempData["AlertMessage"] = "Member Sign Up Successful, Please Login.";
            }
            catch (Exception ex)
            {
                TempData["AlertMessage"] = ex.Message;
            }
        }
    }
}