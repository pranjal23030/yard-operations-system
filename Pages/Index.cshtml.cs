using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace YardOps.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        public IActionResult OnGet()
        {
            if (User.IsInRole("Admin"))
                return RedirectToPage("/Admin/Dashboard");

            if (User.IsInRole("YardManager"))
                return RedirectToPage("/YardManager/Dashboard");

            if (User.IsInRole("Driver"))
                return RedirectToPage("/Driver/Dashboard");

            return RedirectToPage("/Account/AccessDenied", new { area = "Identity" });
        }
    }
}