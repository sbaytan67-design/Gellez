using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GellezWeb.Pages
{
    public class CustomerDashboardModel : PageModel
    {
        public string FullName { get; set; } = "";
        public List<Database.StaffInfo> Stylists { get; set; } = new();

        public IActionResult OnGet()
        {
            var name = HttpContext.Session.GetString("FullName");
            if (name == null) return RedirectToPage("/Login");

            FullName = name;

            try
            {
                Stylists = Database.GetStaffList(true)
                    .Where(s => s.Specialties.Count > 0)
                    .ToList();
            }
            catch
            {
                Stylists = new List<Database.StaffInfo>();
            }

            return Page();
        }
    }
}