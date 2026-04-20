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
        private readonly string _fallbackModel = "gemini-1.5-flash"; // ⭐ fallback model

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

        // ---------------------------------------------------------
        // ⭐ RETRY + FALLBACK WRAPPER
        // ---------------------------------------------------------
        private async Task<string> CallGeminiWithRetryAsync(string model, object payload)
        {
            var json = JsonSerializer.Serialize(payload);
            var endpoint = $"models/{model}:generateContent?key={_apiKey}";
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            for (int attempt = 1; attempt <= 3; attempt++)
            {
                using var resp = await _httpClient.PostAsync(endpoint, content);
                var respText = await resp.Content.ReadAsStringAsync();

                if (resp.IsSuccessStatusCode)
                    return ExtractGeminiText(respText, _logger);

                // ⭐ Handle 503 overload
                if ((int)resp.StatusCode == 503)
                {
                    _logger.LogWarning("Gemini overloaded (503). Attempt {Attempt}/3", attempt);
                    await Task.Delay(500 * attempt);
                    continue;
                }

                // Other errors → throw
                _logger.LogError("Gemini API error: {Status} - {Response}", resp.StatusCode, respText);
                throw new InvalidOperationException($"Gemini API error: {resp.StatusCode} - {respText}");
            }

            // ⭐ If retries fail → fallback model
            _logger.LogWarning("Primary model failed after retries. Falling back to {Fallback}", _fallbackModel);

            return await CallGeminiFallbackAsync(payload);
        }

        private async Task<string> CallGeminiFallbackAsync(object payload)
        {
            var json = JsonSerializer.Serialize(payload);
            var endpoint = $"models/{_fallbackModel}:generateContent?key={_apiKey}";
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var resp = await _httpClient.PostAsync(endpoint, content);
            var respText = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("Fallback Gemini model also failed: {Status} - {Response}", resp.StatusCode, respText);
                throw new InvalidOperationException($"Gemini fallback error: {resp.StatusCode} - {respText}");
            }

            return ExtractGeminiText(respText, _logger);
        }

        // ---------------------------------------------------------
        // ⭐ SHARED HELPERS
        // ---------------------------------------------------------
        private static string BuildPromptFromMessages(object[] messages)
        {
            var sb = new StringBuilder();
            foreach (var msg in messages)
            {
                var roleProp = msg.GetType().GetProperty("role")?.GetValue(msg)?.ToString();
                var contentProp = msg.GetType().GetProperty("content")?.GetValue(msg)?.ToString();
                if (!string.IsNullOrEmpty(roleProp))
                    sb.Append($"[{roleProp}] ");
                if (!string.IsNullOrEmpty(contentProp))
                    sb.AppendLine(contentProp);
            }
            return sb.ToString();
        }

        private static string ExtractGeminiText(string respText, ILogger logger)
        {
            try
            {
                using var doc = JsonDocument.Parse(respText);
                if (doc.RootElement.TryGetProperty("candidates", out var candidates) &&
                    candidates.GetArrayLength() > 0)
                {
                    var first = candidates[0];
                    if (first.TryGetProperty("content", out var contentProp) &&
                        contentProp.TryGetProperty("parts", out var parts) &&
                        parts.GetArrayLength() > 0)
                    {
                        return parts[0].GetProperty("text").GetString() ?? string.Empty;
                    }
                }

                logger.LogError("Gemini API returned unexpected JSON: {Response}", respText);
                return string.Empty;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to parse Gemini response JSON: {Response}", respText);
                throw;
            }
        }

        // ---------------------------------------------------------
        // ⭐ GENERAL CHAT
        // ---------------------------------------------------------
        public async Task<string> SendGeneralPromptAsync(string userMessage)
        {
            var prompt = new[]
            {
                new { role = "system", content = "You are a helpful AI assistant. Respond conversationally and clearly." },
                new { role = "user", content = userMessage }
            };

            var payload = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = BuildPromptFromMessages(prompt) } } }
                }
            };

            return await CallGeminiWithRetryAsync(_model, payload);
        }

        // ---------------------------------------------------------
        // ⭐ AskGeminiAsync (now uses retry + fallback)
        // ---------------------------------------------------------
        public async Task<string> AskGeminiAsync(string prompt)
        {
            var payload = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };

            return await CallGeminiWithRetryAsync(_model, payload);
        }

        // ---------------------------------------------------------
        // ⭐ All other methods now use AskGeminiAsync internally
        // ---------------------------------------------------------
        public async Task<string> SendCurriculumAsync(curriculum cur)
        {
            var userPrompt = new[]
            {
                new { role = "system", content = "You are a precise JSON-output assistant. Return only valid JSON." },
                new { role = "user", content = "Given the curriculum JSON below, return a JSON object with: department_id, Dname, major_name, catalog_year, total_credits, sections_count, first_three_course_names." },
                new { role = "user", content = JsonSerializer.Serialize(cur) }
            };

            return await AskGeminiAsync(BuildPromptFromMessages(userPrompt));
        }

        public async Task<string> SendPromptWithCurriculumAsync(curriculum cur, string userQuestion)
        {
            var messages = new[]
            {
                new { role = "system", content = "You are a helpful assistant that answers questions about curriculum data." },
                new { role = "user", content = userQuestion },
                new { role = "user", content = JsonSerializer.Serialize(cur) }
            };

            return await AskGeminiAsync(BuildPromptFromMessages(messages));
        }

        public async Task<string> DigestPdfToMenuJsonAsync(string pdfText, string menuName)
        {
            var prompt = $@"
You are a precise JSON-output assistant.
Convert the following MENU text into a JSON object matching EXACTLY this schema:

{{ ... schema omitted for brevity ... }}

MENU TEXT:
{pdfText}
";

            return await AskGeminiAsync(prompt);
        }

        public async Task<string> SendPromptWithMenuItemAsync(MenuItem item, string userQuestion)
        {
            var prompt = $@"
Menu Item: {item.name}
Description: {item.description}
Ingredients: {string.Join(", ", item.ingredients)}
Calories: {item.calories}
Price: ${item.price}

User Question: {userQuestion}
";

            return await AskGeminiAsync(prompt);
        }

        public async Task<string> SendPromptWithMenuListAsync(List<MenuItem> items, string userQuestion)
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are an allergy-aware restaurant assistant.");
            sb.AppendLine("MENU:");

            foreach (var item in items)
            {
                sb.AppendLine($"Name: {item.name}");
                sb.AppendLine($"Ingredients: {string.Join(", ", item.ingredients ?? new List<string>())}");
                sb.AppendLine();
            }

            sb.AppendLine("User question:");
            sb.AppendLine(userQuestion);

            return await AskGeminiAsync(sb.ToString());
        }
    }
}
