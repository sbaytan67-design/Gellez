using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GellezWeb.Pages
{
    public class OwnerCreateModel : PageModel
    {
        private readonly IConfiguration _config;

        public OwnerCreateModel(IConfiguration config)
        {
            _config = config;
        }

        [BindProperty] public string? FullName { get; set; }
        [BindProperty] public string? Email { get; set; }
        [BindProperty] public string? Role { get; set; } = "Staff";
        [BindProperty] public string? Password { get; set; }
        [BindProperty] public string? ConfirmPassword { get; set; }
        [BindProperty] public string? AccessCode { get; set; }

        public string? ErrorMessage { get; set; }
        public bool Success { get; set; }

        public void OnGet() { }

        public IActionResult OnPost()
        {
            string name = (FullName ?? "").Trim();
            string email = (Email ?? "").Trim().ToLower();
            string password = Password ?? "";
            string role = Role ?? "";

            if (name == "" || email == "" || password == "")
            {
                ErrorMessage = "Please fill in all fields.";
                return Page();
            }

            if (role != "Owner" && role != "Staff")
            {
                ErrorMessage = "Please choose a role.";
                return Page();
            }

            string? expected = _config["OwnerSignupCode"];
            if (string.IsNullOrEmpty(expected))
            {
                ErrorMessage = "Sign-up is not set up yet. Please contact the administrator.";
                return Page();
            }
            if ((AccessCode ?? "").Trim() != expected)
            {
                ErrorMessage = "Invalid access code.";
                return Page();
            }

            if (!MailAddress.TryCreate(email, out _))
            {
                ErrorMessage = "Please enter a valid email address.";
                return Page();
            }

            if (password.Length < 6)
            {
                ErrorMessage = "Password must be at least 6 characters.";
                return Page();
            }

            if (password != ConfirmPassword)
            {
                ErrorMessage = "Passwords do not match.";
                return Page();
            }

            try
            {
                if (Database.StaffEmailExists(email))
                {
                    ErrorMessage = "This email is already registered.";
                    return Page();
                }

                Database.CreateStaff(name, email, password, role);

                // Awtomatikong naka-login, deretso sa dashboard
                HttpContext.Session.SetString("StaffName", name);
                HttpContext.Session.SetString("StaffRole", role);
                HttpContext.Session.SetString("StaffEmail", email);

                // Ang bagong Staff na wala pang napipiling serbisyo ay dadalhin muna ng dashboard sa form ng serbisyo
                return RedirectToPage(role == "Owner" ? "/OwnerDashboard" : "/StaffDashboard");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Database error: " + ex.Message;
            }

            return Page();
        }
    }
}