using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GellezWeb.Pages
{
    public class StaffAccountsModel : PageModel
    {
        public List<Database.StaffInfo> StaffList { get; set; } = new();

        public IActionResult OnGet()
        {
            if (HttpContext.Session.GetString("StaffName") == null) return RedirectToPage("/OwnerLogin");
            if ((HttpContext.Session.GetString("StaffRole") ?? "") != "Owner") return RedirectToPage("/StaffDashboard");

            StaffList = Database.GetStaffList(true);
            return Page();
        }
    }
}