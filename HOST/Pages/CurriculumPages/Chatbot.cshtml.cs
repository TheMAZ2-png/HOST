using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HOST.Extensions;
using HOST.Models;
using HOST.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace HOST.Pages.CurriculumPages
{
    public class ChatbotModel : PageModel
    {
        private readonly AIService _ai;
        private readonly ILogger<ChatbotModel> _logger;
        private readonly MongoDBService _mongo;

        public ChatbotModel(AIService ai, ILogger<ChatbotModel> logger, MongoDBService mongo)
        {
            _ai = ai;
            _logger = logger;
            _mongo = mongo;
        }

        [BindProperty]
        public string UserQuestion { get; set; }

        public List<ChatMessage> ChatHistory { get; set; } = new();

        public async Task OnGetAsync()
        {
            LoadChatHistory();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            LoadChatHistory();

            if (!string.IsNullOrWhiteSpace(UserQuestion))
            {
                // Add user message
                ChatHistory.Add(new ChatMessage
                {
                    Role = "user",
                    Text = UserQuestion
                });

                var lower = UserQuestion.ToLower();

                //  Decide if this is a menu-related question
                bool isMenuQuestion =
                    lower.Contains("menu") ||
                    lower.Contains("specials") ||
                    lower.Contains("item") ||
                    lower.Contains("eat") ||
                    lower.Contains("order") ||
                    lower.Contains("allergy") ||
                    lower.Contains("allergies") ||
                    lower.Contains("gluten") ||
                    lower.Contains("dairy") ||
                    lower.Contains("egg") ||
                    lower.Contains("eggs") ||
                    lower.Contains("nut") ||
                    lower.Contains("nuts") ||
                    lower.Contains("peanut") ||
                    lower.Contains("peanuts") ||
                    lower.Contains("vegan") ||
                    lower.Contains("vegetarian") ||
                    lower.Contains("calorie") ||
                    lower.Contains("calories") ||
                    lower.Contains("under ") ||
                    lower.Contains("less than");

                string response;

                if (isMenuQuestion)
                {
                    //  Load menu from MongoDB
                    var menus = await _mongo.GetAllAsync();

                    // Flatten categories → items
                    var menuItems = menus
                        .Where(m => m.categories != null)
                        .SelectMany(m => m.categories)
                        .Where(c => c.items != null)
                        .SelectMany(c => c.items)
                        .ToList();

                    // Let the AI reason over the full menu + question
                    response = await _ai.SendPromptWithMenuListAsync(menuItems, UserQuestion);
                }
                else
                {
                    // Non-menu question → general chat
                    response = await _ai.SendGeneralPromptAsync(UserQuestion);
                }

                // Add AI response
                ChatHistory.Add(new ChatMessage
                {
                    Role = "ai",
                    Text = response
                });

                SaveChatHistory();
            }

            return Page();
        }

        private void LoadChatHistory()
        {
            var stored = HttpContext.Session.GetObject<List<ChatMessage>>("ChatHistory");
            if (stored != null)
                ChatHistory = stored;
        }

        private void SaveChatHistory()
        {
            HttpContext.Session.SetObject("ChatHistory", ChatHistory);
        }
    }

    public class ChatMessage
    {
        public string Role { get; set; }
        public string Text { get; set; }
    }
}
