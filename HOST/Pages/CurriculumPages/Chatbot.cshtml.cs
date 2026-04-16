using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HOST.Models;
using HOST.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace HOST.Pages.CurriculumPages
{
    public class ChatbotModel : PageModel
    {
        private readonly MongoDBService _mongo;
        private readonly AIService _ai;
        private readonly ILogger<ChatbotModel> _logger;

        public ChatbotModel(MongoDBService mongo, AIService ai, ILogger<ChatbotModel> logger)
        {
            _mongo = mongo;
            _ai = ai;
            _logger = logger;
        }

        public List<MenuItem> Items { get; private set; } = new();

        [BindProperty]
        public string SelectedItemId { get; set; }

        [BindProperty]
        public string UserQuestion { get; set; }

        public string AIResponse { get; private set; }

        public bool IsProcessing { get; private set; }

        public async Task OnGetAsync()
        {
            var menu = await _mongo.GetAsync("SPECIALS_MENU");

            Items = menu.categories
                        .SelectMany(c => c.items)
                        .ToList();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            IsProcessing = true;

            try
            {
                var menu = await _mongo.GetAsync("SPECIALS_MENU");

                Items = menu.categories
                            .SelectMany(c => c.items)
                            .ToList();

                if (string.IsNullOrWhiteSpace(SelectedItemId))
                {
                    ModelState.AddModelError(string.Empty, "Please select a menu item.");
                    return Page();
                }

                var item = Items.FirstOrDefault(i => i.item_id == SelectedItemId);

                if (item == null)
                {
                    ModelState.AddModelError(string.Empty, "Selected item not found.");
                    return Page();
                }

                if (string.IsNullOrWhiteSpace(UserQuestion))
                {
                    ModelState.AddModelError(string.Empty, "Please enter a question.");
                    return Page();
                }

                AIResponse = await _ai.SendPromptWithMenuItemAsync(item, UserQuestion);

                return Page();
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Chatbot request failed.");
                ModelState.AddModelError(string.Empty, "Failed to get response from AI: " + ex.Message);
                return Page();
            }
            finally
            {
                IsProcessing = false;
            }
        }
    }
}
