using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GellezWeb.Pages
{
    public class MyAppointmentsModel : PageModel
    {
        public List<Database.CustAppt> Items { get; set; } = new();
        [TempData] public string? Message { get; set; }
        [TempData] public string? Error { get; set; }
        public string? ErrorMessage { get; set; }

        public IActionResult OnGet()
        {
            var email = HttpContext.Session.GetString("Email");
            if (HttpContext.Session.GetString("FullName") == null || email == null)
                return RedirectToPage("/Login");

            try
            {
                Items = Database.GetCustomerAppointments(email, true);
            }
            catch (Exception ex)
            {
                ErrorMessage = "Database error: " + ex.Message;
            }
            return Page();
        }

        public IActionResult OnPostCancel(int id)
        {
            var email = HttpContext.Session.GetString("Email");
            if (HttpContext.Session.GetString("FullName") == null || email == null)
                return RedirectToPage("/Login");

            try
            {
                if (Database.CancelAppointment(id, email))
                    Message = "Your appointment was cancelled.";
                else
                    Error = "This appointment can no longer be cancelled (less than 2 hours left, or already cancelled).";
            }
            catch (Exception ex)
            {
                Error = "Database error: " + ex.Message;
            }
            return RedirectToPage();
        }
    }
}