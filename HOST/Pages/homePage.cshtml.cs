using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;

namespace HOST.Pages
{
    [Authorize(Roles = "Manager,Host,Server")]

    public class homePageModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
