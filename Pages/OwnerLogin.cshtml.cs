using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GellezWeb.Pages
{
    public class OwnerLoginModel : PageModel
    {
        [BindProperty] public string? Email { get; set; }
        [BindProperty] public string? Password { get; set; }
        public string? ErrorMessage { get; set; }

        public void OnGet() { }

        public IActionResult OnPost()
        {
            try
            {
                string email = (Email ?? "").Trim().ToLower();
                var user = Database.CheckStaffLogin(email, Password ?? "");

                if (user != null)
                {
                    HttpContext.Session.SetString("StaffName", user.Value.FullName);
                    HttpContext.Session.SetString("StaffRole", user.Value.Role);
                    HttpContext.Session.SetString("StaffEmail", email);
                    return RedirectToPage(user.Value.Role == "Owner" ? "/OwnerDashboard" : "/StaffDashboard");
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