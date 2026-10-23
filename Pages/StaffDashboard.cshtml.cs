using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Reflection.Metadata.Ecma335;

namespace GellezWeb.Pages
{
    public class StaffDashboardModel : PageModel
    {
        public string FullName { get; set; } = "";
        public string Role { get; set; } = "";
        public IActionResult OnGet()
        {
            var name = HttpContext.Session.GetString("StaffName");
            if (name == null) return RedirectToPage("/OwnerLogin");

            Role = HttpContext.Session.GetString("StaffRole") ?? "";
            if (Role == "Owner") return RedirectToPage("/OwnerDashboard");

            var staffEmail = HttpContext.Session.GetString("StaffEmail") ?? "";
            if (Database.GetSpecialties(staffEmail).Count == 0)
                return RedirectToPage("/StaffSpecialties");

            FullName = name;
            return Page();
        }

        public IActionResult OnGetSchedule()
        {
            var name = HttpContext.Session.GetString("StaffName");
            if (name == null) return new UnauthorizedResult();

            var email = HttpContext.Session.GetString("StaffEmail");

            try
            {
                var items = Database.GetTodaysAppointments(email, false);
                return new JsonResult(items.Select(a => new
                {
                    id = a.Id,
                    time = a.When.ToString("h:mm tt"),
                    customer = a.CustomerName,
                    service = a.Service,
                    stylist = a.StylistName,
                    status = a.Status
                }));
            }
            catch(Exception ex)
            {
                return new JsonResult(new { error = ex.Message }) { StatusCode = 500 };
            }
        }

        public IActionResult OnPostAddSample()
        {
            if (HttpContext.Session.GetString("StaffName") == null) return RedirectToPage("/OwnerLogin");

            var rnd = new Random();
            string[] services = { "Haircut", "Hair Color", "Rebond", "Curl", "Hair Treatment", "Manicure", "Pedicure", "Foot Spa" };
            string[] names = { "Marilyn", "Irene", "Ian", "Grace", "Michelle" };
            var when = DateTime.Today.AddHours(9 + rnd.Next(0, 9)).AddMinutes(rnd.Next(0, 2) * 30);

            Database.AddAppointment(
                names[rnd.Next(names.Length)] + " (sample)",
                "sample@example.com",
                services[rnd.Next(services.Length)],
                HttpContext.Session.GetString("StaffEmail"),
                HttpContext.Session.GetString("StaffName"),
                when);

            return RedirectToPage();
        }

        public IActionResult OnPostClearSample()
        {
            if (HttpContext.Session.GetString("StaffName") == null) return RedirectToPage("/OwnerLogin");
            Database.ClearSampleAppointments();
            return RedirectToPage();
        }

        public IActionResult OnPostLogout()
        {
            HttpContext.Session.Clear();
            return Redirect("/");
        }
    }
}
