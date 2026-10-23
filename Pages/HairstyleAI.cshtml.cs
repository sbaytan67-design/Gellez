using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GellezWeb.Pages
{
    public class HairstyleAIModel : PageModel
    {
        public Database.HairRecItem? Last { get; set; }

        public IActionResult OnGet()
        {
            if (HttpContext.Session.GetString("FullName") == null)
                return RedirectToPage("/Login");

            var email = HttpContext.Session.GetString("Email");
            if (email != null)
            {
                try { Last = Database.GetLatestHairRecommendation(email); }
                catch { Last = null; }
            }
            return Page();
        }

        public IActionResult OnPostSave(string faceShape, string? chosenStyle, string recommended)
        {
            var email = HttpContext.Session.GetString("Email");
            if (HttpContext.Session.GetString("FullName") == null || email == null)
                return new UnauthorizedResult();

            string[] shapes = { "Oval", "Round", "Square", "Heart", "Oblong" };
            if (!shapes.Contains(faceShape)) return new BadRequestResult();

            string? style = string.IsNullOrWhiteSpace(chosenStyle) ? null : chosenStyle.Trim();
            if (style != null && style.Length > 100) style = style.Substring(0, 100);

            string rec = (recommended ?? "").Trim();
            if (rec.Length > 500) rec = rec.Substring(0, 500);

            try
            {
                Database.SaveHairRecommendation(email, faceShape, style, rec);
                return new JsonResult(new { ok = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { ok = false, error = ex.Message }) { StatusCode = 500 };
            }
        }
    }
}