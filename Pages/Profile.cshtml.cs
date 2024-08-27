using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace book_management.Pages
{
    public class ProfileModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public ProfileModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty]
        public UserProfile UserProfile { get; set; }

        public string ErrorMessage { get; set; }
        public string SuccessMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (HttpContext.Session.GetString("role") == null || HttpContext.Session.GetString("role") == "")
            {
                return RedirectToPage("/Login");
            }

            await LoadUserProfileAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                if (Request.Form.ContainsKey("Delete"))
                {
                    await DeleteUserProfileAsync(UserProfile.MemberId);
                    HttpContext.Session.Clear();
                    return RedirectToPage("/Default");
                }
                else
                {
                    await UpdateUserProfileAsync(UserProfile);
                    SuccessMessage = "Profile updated successfully.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            return Page();
        }

        private async Task LoadUserProfileAsync()
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("con");
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await con.OpenAsync();
                    SqlCommand cmd = new SqlCommand("SELECT * FROM member WHERE member_id = @member_id;", con);
                    cmd.Parameters.AddWithValue("@member_id", HttpContext.Session.GetString("username"));

                    SqlDataReader reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        UserProfile = new UserProfile
                        {
                            Name = reader["name"].ToString(),
                            Dob = reader["dob"].ToString(),
                            Contact = reader["contact"].ToString(),
                            Email = reader["email"].ToString(),
                            MemberId = reader["member_id"].ToString(),
                            Password = reader["password"].ToString(),
                            Province = reader["province"].ToString()
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        private async Task UpdateUserProfileAsync(UserProfile userProfile)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("con");
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await con.OpenAsync();
                    SqlCommand cmd = new SqlCommand("UPDATE member SET name=@name, dob=@dob, contact=@contact, email=@email, province=@province, password=@password WHERE member_id=@member_id", con);
                    cmd.Parameters.AddWithValue("@name", userProfile.Name);
                    cmd.Parameters.AddWithValue("@dob", userProfile.Dob);
                    cmd.Parameters.AddWithValue("@contact", userProfile.Contact);
                    cmd.Parameters.AddWithValue("@email", userProfile.Email);
                    cmd.Parameters.AddWithValue("@province", userProfile.Province);
                    cmd.Parameters.AddWithValue("@password", userProfile.Password);
                    cmd.Parameters.AddWithValue("@member_id", userProfile.MemberId);

                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
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

        public class UserProfile
        {
            public string Name { get; set; }
            public string Dob { get; set; }
            public string Contact { get; set; }
            public string Email { get; set; }
            public string MemberId { get; set; }
            public string Password { get; set; }
            public string Province { get; set; }
        }
    }
}
