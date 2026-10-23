using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GellezWeb.Pages
{
    public class StaffSpecialtiesModel : PageModel
    {
        public List<string> AllSpecialties => Database.GetServices().Select(s => s.Name).ToList();
        [BindProperty] public List<string> Selected { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public bool IsFirstTime { get; set; }

        private string? CurrentStaffEmail()
        {
            if ((HttpContext.Session.GetString("StaffRole") ?? "") != "Staff") return null;
            return HttpContext.Session.GetString("StaffEmail");
        }

        public IActionResult OnGet()
        {
            var email = CurrentStaffEmail();
            if (email == null) return RedirectToPage("/OwnerLogin");

            Selected = Database.GetSpecialties(email);
            IsFirstTime = Selected.Count == 0;
            return Page();
        }

        public IActionResult OnPost()
        {
            var email = CurrentStaffEmail();
            if (email == null) return RedirectToPage("/OwnerLogin");

            var allowed = AllSpecialties;
            var valid = (Selected ?? new List<string>())
                .Where(s => allowed.Contains(s))
                .Distinct()
                .ToList();

            bool first = Database.GetSpecialties(email).Count == 0;

            if (valid.Count == 0)
            {
                ErrorMessage = "Please select at least one service.";
                Selected = new List<string>();
                IsFirstTime = first;
                return Page();
            }

            try
            {
                Database.SaveSpecialties(email, valid);
            }
            catch (Exception ex)
            {
                ErrorMessage = "Database error: " + ex.Message;
                IsFirstTime = first;
                return Page();
            }

            return RedirectToPage(first ? "/StaffDashboard" : "/StaffProfile");
        }
    }
}