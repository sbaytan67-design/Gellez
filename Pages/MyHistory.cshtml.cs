using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GellezWeb.Pages
{
    public class MyHistoryModel : PageModel
    {
        public List<Database.CustAppt> Items { get; set; } = new();
        public decimal TotalSpent { get; set; }
        public string? ErrorMessage { get; set; }

        public IActionResult OnGet()
        {
            var email = HttpContext.Session.GetString("Email");
            if (HttpContext.Session.GetString("FullName") == null || email == null)
                return RedirectToPage("/Login");

            try
            {
                Items = Database.GetCustomerAppointments(email, false);
                TotalSpent = Items.Where(i => i.Status != "Cancelled").Sum(i => i.Price);
            }
            catch (Exception ex)
            {
                ErrorMessage = "Database error: " + ex.Message;
            }
            return Page();
        }
    }
}