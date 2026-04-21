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
            // Load the menu from MongoDB
            Menu = await _mongoService.GetAsync("SPECIALS_MENU");

            // ⭐ FIX: Ensure Menu is never null
            if (Menu == null)
            {
                Menu = new curriculum
                {
                    menu_name = "No menu found",
                    date = "",
                    chef = "",
                    categories = new List<MenuCategory>(),   // ⭐ Correct type
                    documents = new List<MenuDocument>(),
                    last_updated = ""
                };
            }
        }
    }
}
