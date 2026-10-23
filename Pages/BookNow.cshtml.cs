using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GellezWeb.Pages
{
    public class BookNowModel : PageModel
    {
        // Oras ng salon (pwede mong baguhin)
        public const int OpenMinutes = 9 * 60;        // pinakaunang oras: 9:00 AM
        public const int LastStartMinutes = 17 * 60;  // huling simula: 5:00 PM
        public const int StepMinutes = 30;            // pwedeng piliin kada 30 minuto (dapat multiple of 5)
        public const int AppointmentMinutes = 60;     // tagal ng bawat appointment
        public const int DaysAhead = 14;              // ilang araw ang lalabas na pagpipilian

        public record SlotItem(string Value, string Label, bool Available);

        public List<Database.ServiceItem> Services { get; set; } = new();
        public Database.ServiceItem? SelectedService { get; set; }
        public List<Database.StaffInfo> Stylists { get; set; } = new();
        public string? StylistKey { get; set; }
        public Database.StaffInfo? SelectedStylist { get; set; }
        public List<DateTime> Dates { get; set; } = new();
        public DateTime? SelectedDate { get; set; }
        public List<SlotItem> Slots { get; set; } = new();
        public string? TimeKey { get; set; }
        public DateTime? When { get; set; }
        public string? AssignedName { get; set; }
        public string? AssignedEmail { get; set; }

        [TempData] public string? Flash { get; set; }
        public string? ErrorMessage { get; set; }

        private static bool Overlaps(DateTime a, DateTime b)
        {
            return Math.Abs((a - b).TotalMinutes) < AppointmentMinutes;
        }

        public IActionResult OnGet(string? service, string? stylist, string? date, string? time)
        {
            if (HttpContext.Session.GetString("FullName") == null) return RedirectToPage("/Login");

            try
            {
                Build(service, stylist, date, time);
            }
            catch (Exception ex)
            {
                ErrorMessage = "Database error: " + ex.Message;
            }
            return Page();
        }

        public IActionResult OnPostConfirm(string service, string stylist, string date, string time)
        {
            var name = HttpContext.Session.GetString("FullName");
            var email = HttpContext.Session.GetString("Email");
            if (name == null || email == null) return RedirectToPage("/Login");

            try
            {
                Build(service, stylist, date, time);

                if (SelectedService == null || StylistKey == null || SelectedDate == null ||
                    TimeKey == null || When == null || AssignedEmail == null)
                {
                    Flash = "That time is no longer available. Please choose again.";
                    return RedirectToPage(new { service, stylist, date });
                }

                if (Database.BookAppointment(name, email, SelectedService.Name, SelectedService.Price,
                        AssignedEmail, AssignedName, When.Value, out var err))
                {
                    TempData["Message"] = "Booked! " + SelectedService.Name + " with " + AssignedName +
                        " on " + When.Value.ToString("MMM d") + " at " + When.Value.ToString("h:mm tt") + ".";
                    return RedirectToPage("/MyAppointments");
                }

                Flash = err;
                return RedirectToPage(new { service, stylist, date });
            }
            catch (Exception ex)
            {
                Flash = "Database error: " + ex.Message;
                return RedirectToPage(new { service, stylist, date });
            }
        }

        private void Build(string? service, string? stylist, string? date, string? time)
        {
            Services = Database.GetServices();

            if (string.IsNullOrEmpty(service)) return;
            SelectedService = Services.FirstOrDefault(s => string.Equals(s.Name, service, StringComparison.OrdinalIgnoreCase));
            if (SelectedService == null) return;

            Stylists = Database.GetEligibleStylists(SelectedService.Name);

            if (string.IsNullOrEmpty(stylist)) return;
            if (stylist == "any")
            {
                StylistKey = "any";
            }
            else
            {
                SelectedStylist = Stylists.FirstOrDefault(s => s.Email == stylist);
                if (SelectedStylist == null) return;
                StylistKey = SelectedStylist.Email;
            }

            for (int i = 0; i < DaysAhead; i++) Dates.Add(DateTime.Today.AddDays(i));

            if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day)) return;
            day = day.Date;
            if (day < DateTime.Today || day > DateTime.Today.AddDays(60)) return;
            SelectedDate = day;

            var email = HttpContext.Session.GetString("Email") ?? "";
            var booked = Database.GetBookedSlots(day);
            var eligible = (StylistKey == "any") ? Stylists : new List<Database.StaffInfo> { SelectedStylist! };
            var earliest = DateTime.Now.AddMinutes(30);

            for (int m = OpenMinutes; m <= LastStartMinutes; m += StepMinutes)
            {
                var t = day.AddMinutes(m);
                bool free = t >= earliest
                    && eligible.Any(s => !booked.Any(b => b.StylistEmail == s.Email && Overlaps(b.When, t)))
                    && !booked.Any(b => b.CustomerEmail == email && Overlaps(b.When, t));
                Slots.Add(new SlotItem(t.ToString("HH:mm", CultureInfo.InvariantCulture), t.ToString("h:mm tt"), free));
            }

            if (string.IsNullOrEmpty(time)) return;
            var slot = Slots.FirstOrDefault(s => s.Value == time && s.Available);
            if (slot == null) return;
            if (!DateTime.TryParseExact(time, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var tod)) return;

            var when = day.Add(tod.TimeOfDay);
            Database.StaffInfo? chosen;
            if (StylistKey == "any")
            {
                chosen = eligible
                    .Where(s => !booked.Any(b => b.StylistEmail == s.Email && Overlaps(b.When, when)))
                    .OrderBy(s => booked.Count(b => b.StylistEmail == s.Email))
                    .ThenBy(s => s.FullName)
                    .FirstOrDefault();
            }
            else
            {
                chosen = SelectedStylist;
            }
            if (chosen == null) return;

            TimeKey = time;
            When = when;
            AssignedName = chosen.FullName;
            AssignedEmail = chosen.Email;
        }
    }
}