using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GellezWeb.Pages
{
    public class LoginModel : PageModel
    {
        [BindProperty] public string? Email { get; set; }
        [BindProperty] public string? Password { get; set; }
        public string? ErrorMessage { get; set; }
        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            try
            {
                string email = (Email ?? "").Trim().ToLower();
                string password = Password ?? "";

                string? fullName = Database.CheckLogin(email, password);

                if (fullName != null)
                {
                    HttpContext.Session.SetString("FullName", fullName);
                    HttpContext.Session.SetString("Email", email);
                    return RedirectToPage("/CustomerDashboard");
                }
                ErrorMessage = "Invalid email or password.";
            }
            catch (Exception ex)
            {
                ErrorMessage = "Database error: " + ex.Message;
            }
            return Page();
        }
    }
}
