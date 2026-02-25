using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HOST.Pages
{
    [AllowAnonymous]
    public class staffCreationPageModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
