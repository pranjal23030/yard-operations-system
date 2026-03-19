using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace YardOps.Pages.Driver
{
    [Authorize(Roles = "Driver")]
    public class DashboardModel : PageModel
    {
        public void OnGet()
        {
            ViewData["Title"] = "Driver Dashboard";
            ViewData["PageHeader"] = "Driver Dashboard";
        }
    }
}