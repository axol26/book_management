using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace book_management.Pages
{
    public class SignUpModel : PageModel
    {
        [BindProperty]
        public string FullName { get; set; }
        
        [BindProperty]
        public DateTime DateOfBirth { get; set; }
        
        [BindProperty]
        public string ContactNumber { get; set; }
        
        [BindProperty]
        public string EmailId { get; set; }
        
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
            // Handle sign-up logic here
            // For example, save the data to the database
            
            // Redirect to another page or show a success message
            return RedirectToPage("Success"); // Change to your success page
        }
    }
}