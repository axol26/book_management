using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace book_management.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(IConfiguration configuration, ILogger<LoginModel> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        // ... existing code ...

        public List<MemberInfo> Members { get; set; }
        public string ReturnUrl { get; set; }

        [BindProperty]
        public string MemberId { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public class MemberInfo
        {
            public string MemberId { get; set; }
            public string Password { get; set; }
        }

        public IActionResult OnPost(string returnUrl = null)
        {
            returnUrl ??= Url.Page("/Profile");

            if (ModelState.IsValid)
            {
                if (CheckIfMemberExists())
                {
                    // Set session variables
                    HttpContext.Session.SetString("username", MemberId);
                    HttpContext.Session.SetString("role", "member");

                    return LocalRedirect(returnUrl);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    _logger.LogWarning("Invalid login attempt for user {UserId}", MemberId);
                }
            }

            return Page();
        }

        private bool CheckIfMemberExists()
        {
            try
            {
                // Debug connection string
                var debugConnectionString = _configuration.GetConnectionString("DefaultConnection");
                _logger.LogInformation($"Using connection string: {debugConnectionString}");

                using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("SELECT * FROM member WHERE member_id = @member_id AND password = @password;", con);
                    cmd.Parameters.AddWithValue("@member_id", MemberId);
                    cmd.Parameters.AddWithValue("@password", Password);

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);

                        // Debug query results
                        _logger.LogInformation($"Found {dt.Rows.Count} matching records for user {MemberId}");
                        
                        if (dt.Rows.Count > 0)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Database error: {ex.Message}");
                throw;
            }
        }
    }

    // public class LoginModel : PageModel
    // {
    //     // ... existing code ...
    //     private readonly IConfiguration _configuration;

    //     public LoginModel(IConfiguration configuration)
    //     {
    //         _configuration = configuration;
    //     }
    //     public List<MemberInfo> Members { get; set; }

    //     public class MemberInfo
    //     {
    //         public string MemberId { get; set; }
    //         public string Password { get; set; }
    //     }

    //     public IActionResult OnGet()
    //     {
    //         Members = GetAllMembers();
    //         return Page();
    //     }

    //     // ... existing methods ...

    //     private List<MemberInfo> GetAllMembers()
    //     {
    //         List<MemberInfo> members = new List<MemberInfo>();
    //         try
    //         {
    //             string strcon = _configuration.GetConnectionString("DefaultConnection");
    //             using (SqlConnection con = new SqlConnection(strcon))
    //             {
    //                 con.Open();
    //                 using (SqlCommand cmd = new SqlCommand("SELECT member_id, password FROM member;", con))
    //                 {
    //                     using (SqlDataReader reader = cmd.ExecuteReader())
    //                     {
    //                         while (reader.Read())
    //                         {
    //                             members.Add(new MemberInfo
    //                             {
    //                                 MemberId = reader["member_id"].ToString(),
    //                                 Password = reader["password"].ToString()
    //                             });
    //                         }
    //                     }
    //                 }
    //             }
    //         }
    //         catch (Exception ex)
    //         {
    //             ModelState.AddModelError(string.Empty, $"Error: {ex.Message}");
    //         }
    //         return members;
    //     }
    // }
}

namespace book_management.Pages
{
    public class LogoutModel : PageModel
    {
        public IActionResult OnGet()
        {
            // Clear all session data
            HttpContext.Session.Clear();
            
            // Redirect to home page
            return RedirectToPage("/Index");
        }
    }
}