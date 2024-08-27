using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace book_management.Pages
{
    public class MemberManagementModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public MemberManagementModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public List<Member> Members { get; set; }
        public string ErrorMessage { get; set; }
        public string SuccessMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (HttpContext.Session.GetString("role") != "admin")
            {
                return RedirectToPage("/AdminLogin");
            }

            await LoadMembersAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(string memberId)
        {
            if (string.IsNullOrEmpty(memberId))
            {
                ErrorMessage = "Member ID is required.";
                await LoadMembersAsync();
                return Page();
            }

            try
            {
                await DeleteUserProfileAsync(memberId);
                SuccessMessage = "Member deleted successfully.";
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            await LoadMembersAsync();
            return Page();
        }

        private async Task DeleteUserProfileAsync(string memberId)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("con");
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await con.OpenAsync();
                    SqlCommand cmd = new SqlCommand("DELETE FROM member WHERE member_id=@member_id", con);
                    cmd.Parameters.AddWithValue("@member_id", memberId);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        private async Task LoadMembersAsync()
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("con");
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await con.OpenAsync();
                    SqlCommand cmd = new SqlCommand("SELECT member_id, member_name FROM member", con);
                    SqlDataReader reader = await cmd.ExecuteReaderAsync();
                    Members = new List<Member>();

                    while (await reader.ReadAsync())
                    {
                        Members.Add(new Member
                        {
                            MemberId = reader["member_id"].ToString(),
                            MemberName = reader["member_name"].ToString()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        public class Member
        {
            public string MemberId { get; set; }
            public string MemberName { get; set; }
        }
    }
}
