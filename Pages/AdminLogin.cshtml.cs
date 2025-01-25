using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace book_management.Pages
{
    public class AdminLoginModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        public string ErrorMessage { get; set; }
        public List<AdminCredential> AdminCredentials { get; set; } = new();

        public AdminLoginModel(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        [BindProperty]
        public string AdminId { get; set; }
        
        [BindProperty]
        public string Password { get; set; }

        public void OnGet()
        {
            GetAdminCredentials();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    await con.OpenAsync();
                    string query = "SELECT * FROM admin WHERE admin_id = @admin_id AND password = @password;";
                    
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@admin_id", AdminId?.Trim());
                        cmd.Parameters.AddWithValue("@password", Password?.Trim());

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);

                            if (dt.Rows.Count > 0)
                            {
                                // Set session values for admin
                                HttpContext.Session.SetString("username", "Admin");
                                HttpContext.Session.SetString("role", "admin");

                                return RedirectToPage("/Index");
                            }
                            else
                            {
                                ErrorMessage = "Invalid admin credentials.";
                                GetAdminCredentials(); // Reload credentials for display
                                return Page();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                GetAdminCredentials(); // Reload credentials for display
                return Page();
            }
        }

        private void GetAdminCredentials()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    using SqlCommand cmd = new SqlCommand(
                        "SELECT admin_id, password FROM admin", con);

                    using SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    AdminCredentials = dt.AsEnumerable()
                        .Select(row => new AdminCredential
                        {
                            AdminId = row["admin_id"].ToString(),
                            Password = row["password"].ToString()
                        }).ToList();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }
    }

    public class AdminCredential
    {
        public string AdminId { get; set; }
        public string Password { get; set; }
    }
}
