using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using Microsoft.Data.SqlClient;

namespace book_management.Pages
{
    public class MemberManagementModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        public List<Member> Members { get; set; } = new();

        public MemberManagementModel(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        public IActionResult OnGet()
        {
            // Check if user is admin
            if (HttpContext.Session.GetString("role") != "admin")
            {
                return RedirectToPage("/AdminLogin");
            }

            LoadMembers();
            return Page();
        }

        public IActionResult OnPostDelete(string memberId)
        {
            if (HttpContext.Session.GetString("role") != "admin")
            {
                return RedirectToPage("/AdminLogin");
            }

            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    using SqlCommand cmd = new SqlCommand(
                        "DELETE FROM member WHERE member_id=@member_id", con);
                    
                    cmd.Parameters.AddWithValue("@member_id", memberId);
                    cmd.ExecuteNonQuery();
                }

                TempData["Message"] = "Member deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Error deleting member: {ex.Message}";
            }

            return RedirectToPage();
        }

        private void LoadMembers()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    using SqlCommand cmd = new SqlCommand(
                        "SELECT * FROM member", con);

                    using SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    Members = dt.AsEnumerable()
                        .Select(row => new Member
                        {
                            Name = row["name"].ToString(),
                            DateOfBirth = Convert.ToDateTime(row["dob"]),
                            Contact = row["contact"].ToString(),
                            Email = row["email"].ToString(),
                            Country = row["province"].ToString(),
                            MemberId = row["member_id"].ToString(),
                            Password = row["password"].ToString()
                        }).ToList();
                }
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Error loading members: {ex.Message}";
            }
        }
    }

    public class Member
    {
        public string Name { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Contact { get; set; }
        public string Email { get; set; }
        public string Country { get; set; }
        public string MemberId { get; set; }
        public string Password { get; set; }
    }
}
