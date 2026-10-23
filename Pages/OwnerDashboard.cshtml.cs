using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GellezWeb.Pages
{
    public class OwnerDashboardModel : PageModel
    {
        public string FullName { get; set; } = "";

        public IActionResult OnGet()
        {
            var name = HttpContext.Session.GetString("StaffName");
            if (name == null) return RedirectToPage("/OwnerLogin");

            var role = HttpContext.Session.GetString("StaffRole") ?? "";
            if (role != "Owner") return RedirectToPage("/StaffDashboard");

            FullName = name;
            return Page();
        }

        // Lahat ng appointment ngayong araw, para sa Owner
        public IActionResult OnGetSchedule()
        {
            if (HttpContext.Session.GetString("StaffName") == null) return new UnauthorizedResult();
            if ((HttpContext.Session.GetString("StaffRole") ?? "") != "Owner") return new UnauthorizedResult();

            try
            {
                var items = Database.GetTodaysAppointments(null, true);
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
            catch (Exception ex)
            {
                return new JsonResult(new { error = ex.Message }) { StatusCode = 500 };
            }
        }

        // Pansamantalang pang-test (walang naka-assign na stylist)
        public IActionResult OnPostAddSample()
        {
            if ((HttpContext.Session.GetString("StaffRole") ?? "") != "Owner") return RedirectToPage("/OwnerLogin");

            var rnd = new Random();
            string[] services = { "Haircut", "Hair Color", "Rebond", "Hair Spa", "Manicure" };
            string[] names = { "Anna Reyes", "Maria Santos", "John Cruz", "Liza Garcia" };
            var when = DateTime.Today.AddHours(9 + rnd.Next(0, 9)).AddMinutes(rnd.Next(0, 2) * 30);

            Database.AddAppointment(
                names[rnd.Next(names.Length)] + " (sample)",
                "sample@example.com",
                services[rnd.Next(services.Length)],
                null,
                null,
                when);

            return RedirectToPage();
        }

        public IActionResult OnPostClearSamples()
        {
            if ((HttpContext.Session.GetString("StaffRole") ?? "") != "Owner") return RedirectToPage("/OwnerLogin");
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