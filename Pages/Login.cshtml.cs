using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using System.Data.SqlClient;

namespace book_management.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public LoginModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty]
        public string MemberId { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public IActionResult OnGet()
        {
            return Page();
        }

        public IActionResult OnPostLogin()
        {
            if (CheckIfMemberExists())
            {
                TempData["SuccessMessage"] = "Login Successful";
                HttpContext.Session.SetString("username", "someUsername"); // Example
                HttpContext.Session.SetString("name", "someName");
                HttpContext.Session.SetString("role", "user");
                return RedirectToPage("Profile");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Member does not exist");
                return Page();
            }
        }

        public IActionResult OnPostSignUp()
        {
            return RedirectToPage("SignUp");
        }

        private bool CheckIfMemberExists()
        {
            try
            {
                string strcon = _configuration.GetConnectionString("con");
                using (SqlConnection con = new SqlConnection(strcon))
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT * FROM member WHERE member_id = @member_id AND password = @password;", con))
                    {
                        cmd.Parameters.AddWithValue("@member_id", MemberId.Trim());
                        cmd.Parameters.AddWithValue("@password", Password.Trim());

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            return dt.Rows.Count > 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error: {ex.Message}");
                return false;
            }
        }
    }
}