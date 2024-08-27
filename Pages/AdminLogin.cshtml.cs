using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace book_management.Pages
{
    public class AdminLoginModel : PageModel
    {
        private readonly IConfiguration _configuration;
        public string ErrorMessage { get; set; }

        public AdminLoginModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty]
        public string AdminId { get; set; }
        
        [BindProperty]
        public string Password { get; set; }

        public void OnGet()
        {
            // No data needed to be populated on GET
        }

        public async Task<IActionResult> OnPostAsync()
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
                    string query = "SELECT * FROM admin WHERE admin_id = @admin_id AND password = @password;";
                    
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@admin_id", AdminId.Trim());
                        cmd.Parameters.AddWithValue("@password", Password.Trim());

                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        da.Fill(dt);

                        if (dt.Rows.Count > 0)
                        {
                            // Set session values
                            HttpContext.Session.SetString("name", "Admin");
                            HttpContext.Session.SetString("role", "admin");

                            return RedirectToPage("/Default");
                        }
                        else
                        {
                            ErrorMessage = "Admin does not exist.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            return Page();
        }
    }
}
