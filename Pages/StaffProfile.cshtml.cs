using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GellezWeb.Pages
{
    public class StaffProfileModel : PageModel
    {
        public Database.StaffInfo? Me { get; set; }

        public IActionResult OnGet()
        {
            var email = HttpContext.Session.GetString("StaffEmail");
            if (email == null) return RedirectToPage("/OwnerLogin");

            Me = Database.GetStaff(email);
            if (Me == null) return RedirectToPage("/OwnerLogin");

            return Page();
        }

        public IActionResult OnPostLogout()
        {
            HttpContext.Session.Clear();
            return Redirect("/");
        }
    }
}