using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GellezWeb.Pages
{
    public class ManageServicesModel : PageModel
    {
        public List<Database.ServiceItem> Services { get; set; } = new();
        [TempData] public string? Message { get; set; }
        [TempData] public string? Error { get; set; }

        private bool IsOwner()
        {
            return HttpContext.Session.GetString("StaffName") != null
                && (HttpContext.Session.GetString("StaffRole") ?? "") == "Owner";
        }

        public IActionResult OnGet()
        {
            if (!IsOwner()) return RedirectToPage("/OwnerLogin");
            Services = Database.GetServices();
            return Page();
        }

        public IActionResult OnPostAdd(string? name, decimal? price)
        {
            if (!IsOwner()) return RedirectToPage("/OwnerLogin");

            name = (name ?? "").Trim();
            if (name.Length == 0 || name.Length > 100)
            {
                Error = "Please enter a service name (up to 100 characters).";
                return RedirectToPage();
            }
            if (price == null || price < 0 || price > 1000000)
            {
                Error = "Please enter a valid price.";
                return RedirectToPage();
            }

            try
            {
                if (Database.AddService(name, price.Value)) Message = "Service added.";
                else Error = "That service already exists.";
            }
            catch (Exception ex)
            {
                Error = "Database error: " + ex.Message;
            }
            return RedirectToPage();
        }

        public IActionResult OnPostUpdate(int id, decimal? price)
        {
            if (!IsOwner()) return RedirectToPage("/OwnerLogin");

            if (price == null || price < 0 || price > 1000000)
            {
                Error = "Please enter a valid price.";
                return RedirectToPage();
            }

            try
            {
                Database.UpdateServicePrice(id, price.Value);
                Message = "Price updated.";
            }
            catch (Exception ex)
            {
                Error = "Database error: " + ex.Message;
            }
            return RedirectToPage();
        }

        public IActionResult OnPostRemove(int id)
        {
            if (!IsOwner()) return RedirectToPage("/OwnerLogin");

            try
            {
                Database.RemoveService(id);
                Message = "Service removed.";
            }
            catch (Exception ex)
            {
                Error = "Database error: " + ex.Message;
            }
            return RedirectToPage();
        }
    }
}