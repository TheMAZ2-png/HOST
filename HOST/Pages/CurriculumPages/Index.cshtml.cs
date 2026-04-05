using HOST.Models;
using HOST.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HOST.Pages.CurriculumPages
{
    [Authorize]

    public class IndexModel : PageModel
    {
        private readonly MongoDBService _mongoService;

        public curriculum Menu { get; set; }

        public IndexModel(MongoDBService mongoService)
        {
            _mongoService = mongoService;
        }

        public async Task OnGetAsync()
        {
            // Load the single specials menu document
            Menu = await _mongoService.GetAsync("SPECIALS_MENU");
        }
    }
}
