using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using HOST.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace HOST.Services
{
    public class AIService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AIService> _logger;
        private readonly string _apiKey;
        private readonly string _model;

        public AIService(HttpClient httpClient, IConfiguration config, ILogger<AIService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var aiSettings = config.GetSection("AISettings");
            _apiKey = aiSettings["ApiKey"] ?? throw new InvalidOperationException("AISettings:ApiKey is not configured.");
            _model = aiSettings["Model"] ?? "gemini-2.5-flash";
            var baseAddress = aiSettings["BaseAddress"] ?? "https://generativelanguage.googleapis.com/v1beta/";
            _httpClient.BaseAddress = new Uri(baseAddress);
        }

        private static string BuildPromptFromMessages(object[] messages)
        {
            var sb = new StringBuilder();
            foreach (var msg in messages)
            {
                var roleProp = msg.GetType().GetProperty("role")?.GetValue(msg)?.ToString();
                var contentProp = msg.GetType().GetProperty("content")?.GetValue(msg)?.ToString();
                if (!string.IsNullOrEmpty(roleProp))
                {
                    sb.Append($"[{roleProp}] ");
                }
                if (!string.IsNullOrEmpty(contentProp))
                {
                    sb.AppendLine(contentProp);
                }
            }
            return sb.ToString();
        }

        private static string ExtractGeminiText(string respText, ILogger logger)
        {
            try
            {
                using var doc = JsonDocument.Parse(respText);
                if (doc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var first = candidates[0];
                    if (first.TryGetProperty("content", out var contentProp) &&
                        contentProp.TryGetProperty("parts", out var parts) &&
                        parts.GetArrayLength() > 0)
                    {
                        var text = parts[0].GetProperty("text").GetString();
                        return text ?? string.Empty;
                    }
                }
                logger.LogError("Gemini API returned unexpected JSON shape: {Response}", respText);
                return string.Empty;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to parse Gemini response JSON: {Response}", respText);
                throw;
            }
        }

        // ---------------------------------------------------------
        // GENERAL CHAT FUNCTION
        // ---------------------------------------------------------
        public async Task<string> SendGeneralPromptAsync(string userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
                throw new ArgumentNullException(nameof(userMessage));

            var prompt = new[]
            {
                new { role = "system", content = "You are a helpful AI assistant. Respond conversationally and clearly." },
                new { role = "user", content = userMessage }
            };

            var promptText = BuildPromptFromMessages(prompt);

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = promptText }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var endpoint = $"models/{_model}:generateContent?key={_apiKey}";

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var resp = await _httpClient.PostAsync(endpoint, content);
            var respText = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini API error: {Status} - {Response}", resp.StatusCode, respText);
                throw new InvalidOperationException($"Gemini API error: {resp.StatusCode} - {respText}");
            }

            return ExtractGeminiText(respText, _logger);
        }

        // ---------------------------------------------------------
        // EXISTING FUNCTIONS BELOW (unchanged)
        // ---------------------------------------------------------

        public async Task<string> SendCurriculumAsync(curriculum cur)
        {
            if (cur == null) throw new ArgumentNullException(nameof(cur));

            var userPrompt = new[]
            {
                new { role = "system", content = "You are a precise JSON-output assistant. Return only valid JSON, no extra commentary." },
                new { role = "user", content = "Given the curriculum JSON below, return a JSON object with: department_id, Dname, major_name, catalog_year, total_credits, sections_count, first_three_course_names (array)." },
                new { role = "user", content = JsonSerializer.Serialize(cur, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull }) }
            };

            var promptText = BuildPromptFromMessages(userPrompt);

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = promptText }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var endpoint = $"models/{_model}:generateContent?key={_apiKey}";
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var resp = await _httpClient.PostAsync(endpoint, content);
            var respText = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini API error: {Status} - {Response}", resp.StatusCode, respText);
                throw new InvalidOperationException($"Gemini API error: {resp.StatusCode} - {respText}");
            }

            return ExtractGeminiText(respText, _logger);
        }

        public async Task<string> AskGeminiAsync(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                throw new ArgumentNullException(nameof(prompt));

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var endpoint = $"models/{_model}:generateContent?key={_apiKey}";
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var resp = await _httpClient.PostAsync(endpoint, content);
            var respText = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini API error: {Status} - {Response}", resp.StatusCode, respText);
                throw new InvalidOperationException($"Gemini API error: {resp.StatusCode} - {respText}");
            }

            return ExtractGeminiText(respText, _logger);
        }

        public async Task<string> SendPromptWithCurriculumAsync(curriculum cur, string userQuestion)
        {
            if (cur == null) throw new ArgumentNullException(nameof(cur));
            if (string.IsNullOrWhiteSpace(userQuestion)) throw new ArgumentNullException(nameof(userQuestion));

            var messages = new[]
            {
                new { role = "system", content = "You are a helpful assistant that answers questions about curriculum data. Be concise." },
                new { role = "user", content = userQuestion },
                new { role = "user", content = JsonSerializer.Serialize(cur, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull }) }
            };

            var promptText = BuildPromptFromMessages(messages);

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = promptText }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var endpoint = $"models/{_model}:generateContent?key={_apiKey}";
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var resp = await _httpClient.PostAsync(endpoint, content);
            var respText = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini API error: {Status} - {Response}", resp.StatusCode, respText);
                throw new InvalidOperationException($"Gemini API error: {resp.StatusCode} - {respText}");
            }

            return ExtractGeminiText(respText, _logger);
        }

        public async Task<string> DigestPdfToMenuJsonAsync(string pdfText, string menuName)
        {
            if (string.IsNullOrWhiteSpace(pdfText))
                throw new ArgumentNullException(nameof(pdfText));

            var prompt = $@"
You are a precise JSON-output assistant.
Convert the following MENU text into a JSON object matching EXACTLY this schema:

{{
  ""Id"": ""SPECIALS_MENU"",
  ""menu_name"": ""{menuName}"",
  ""date"": ""string"",
  ""chef"": ""string"",
  ""categories"": [
    {{
      ""category_name"": ""string"",
      ""items"": [
        {{
          ""item_id"": ""string"",
          ""name"": ""string"",
          ""description"": ""string"",
          ""ingredients"": [""string""],
          ""calories"": number,
          ""price"": number
        }}
      ]
    }}
  ],
  ""documents"": [],
  ""last_updated"": ""YYYY-MM-DD""
}}

Rules:
- Return ONLY valid JSON.
- Infer missing fields from context.
- Auto-generate item_id values (e.g., EN001, DR001).
- Use today's date for last_updated.
- Do NOT include commentary.

MENU TEXT:
{pdfText}
";

            return await AskGeminiAsync(prompt);
        }


        public async Task<string> SendPromptWithMenuItemAsync(MenuItem item, string userQuestion)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (string.IsNullOrWhiteSpace(userQuestion)) throw new ArgumentNullException(nameof(userQuestion));

            var prompt = $@"
You are an allergy‑aware restaurant assistant.

Menu Item: {item.name}
Description: {item.description}
Ingredients: {string.Join(", ", item.ingredients)}
Calories: {item.calories}
Price: ${item.price}

User Question: {userQuestion}

Provide a clear, safe, helpful answer about allergies, ingredients, or dietary concerns.
";

            return await AskGeminiAsync(prompt);
        }

        // ---------------------------------------------------------
        // ⭐ NEW: FULL MENU REASONING FOR INGREDIENT QUESTIONS
        // ---------------------------------------------------------
        public async Task<string> SendPromptWithMenuListAsync(List<MenuItem> items, string userQuestion)
        {
            if (items == null || items.Count == 0)
                throw new ArgumentNullException(nameof(items));
            if (string.IsNullOrWhiteSpace(userQuestion))
                throw new ArgumentNullException(nameof(userQuestion));

            var sb = new StringBuilder();
            sb.AppendLine("You are an allergy- and ingredient-aware restaurant assistant.");
            sb.AppendLine("You are given the FULL MENU with ingredients. Answer the user's question ONLY using this menu.");
            sb.AppendLine();
            sb.AppendLine("MENU:");

            foreach (var item in items)
            {
                sb.AppendLine($"Name: {item.name}");
                sb.AppendLine($"Description: {item.description}");
                sb.AppendLine($"Ingredients: {string.Join(", ", item.ingredients ?? new List<string>())}");
                sb.AppendLine($"Calories: {item.calories}");
                sb.AppendLine($"Price: {item.price}");
                sb.AppendLine();
            }

            sb.AppendLine("User question:");
            sb.AppendLine(userQuestion);

            return await AskGeminiAsync(sb.ToString());
        }
    }
}
