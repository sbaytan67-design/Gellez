using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GellezWeb.Pages
{
    public class CustomerCreateModel : PageModel
    {
        [BindProperty] public string? FullName { get; set; }
        [BindProperty] public string? Email { get; set; }
        [BindProperty] public string? Password { get; set; }
        [BindProperty] public string? ConfirmPassword { get; set; }

        public string? ErrorMessage { get; set; }
        public bool Success { get; set; } = false;
        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            string name = (FullName ?? "").Trim();
            string email = (Email ?? "").Trim().ToLower();
            string password = Password ?? "";

            if(name == "" || email == "" || password == "")
            {
                ErrorMessage = "Please fill in all fields.";
                return Page();
            }

            if(!MailAddress.TryCreate(email, out _))
            {
                ErrorMessage = "Please enter a valid email address.";
                return Page();
            }

            if (password.Length < 8)
            {
                ErrorMessage = "Password must be at least 8 characters.";
                return Page();
            }

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "Passwords do not match.";
                return Page();
            }

            try
            {
                if (Database.EmailExists(email))
                {
                    ErrorMessage = "This email is already registered.";
                    return Page();
                }


                Database.CreateUser(name, email, password);

                HttpContext.Session.SetString("FullName", name);
                HttpContext.Session.SetString("Email", email);
                return RedirectToPage("/CustomerDashboard");
            }
            catch(Exception ex)
            {
                ErrorMessage = "Database error: " + ex.Message;
            }
            return Page();
        }
    }
}
