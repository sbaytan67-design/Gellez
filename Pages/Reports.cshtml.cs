using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GellezWeb.Pages
{
    public class ReportsModel : PageModel
    {
        public string Period { get; set; } = "day";
        public DateTime Anchor { get; set; } = DateTime.Today;
        public string RangeLabel { get; set; } = "";
        public string PrevUrl { get; set; } = "";
        public string NextUrl { get; set; } = "";
        public Database.ReportData? Data { get; set; }
        public string? ErrorMessage { get; set; }

        public IActionResult OnGet(string? period, string? date)
        {
            if (HttpContext.Session.GetString("StaffName") == null) return RedirectToPage("/OwnerLogin");
            if ((HttpContext.Session.GetString("StaffRole") ?? "") != "Owner") return RedirectToPage("/StaffDashboard");

            Period = (period == "week" || period == "month") ? period : "day";
            Anchor = DateTime.TryParse(date, out var parsed) ? parsed.Date : DateTime.Today;

            DateTime from, to, prev, next;
            if (Period == "week")
            {
                int diff = ((int)Anchor.DayOfWeek + 6) % 7;   // Lunes ang simula ng linggo
                from = Anchor.AddDays(-diff);
                to = from.AddDays(7);
                RangeLabel = from.ToString("MMM d") + " - " + to.AddDays(-1).ToString("MMM d, yyyy");
                prev = from.AddDays(-7);
                next = from.AddDays(7);
            }
            else if (Period == "month")
            {
                from = new DateTime(Anchor.Year, Anchor.Month, 1);
                to = from.AddMonths(1);
                RangeLabel = from.ToString("MMMM yyyy");
                prev = from.AddMonths(-1);
                next = from.AddMonths(1);
            }
            else
            {
                from = Anchor;
                to = from.AddDays(1);
                RangeLabel = from.ToString("dddd, MMM d, yyyy");
                prev = from.AddDays(-1);
                next = from.AddDays(1);
            }

            PrevUrl = MakeUrl(prev);
            NextUrl = MakeUrl(next);

            try
            {
                Data = Database.GetReport(from, to);
            }
            catch (Exception ex)
            {
                ErrorMessage = "Database error: " + ex.Message;
            }

            return Page();
        }

        private string MakeUrl(DateTime d)
        {
            return "/Reports?period=" + Period + "&date=" + d.ToString("yyyy-MM-dd");
        }
    }
}