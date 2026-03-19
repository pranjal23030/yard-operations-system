using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace YardOps.Pages.YardManager
{
    [Authorize(Roles = "YardManager")]
    public class DashboardModel : PageModel
    {
        public void OnGet()
        {
            ViewData["Title"] = "Yard Manager Dashboard";
            ViewData["PageHeader"] = "Yard Manager Dashboard";
        }
    }
}