using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using HOST.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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

        // ---------------------------------------------------------
        // NEW METHOD: SendPromptWithMenuItemAsync
        // ---------------------------------------------------------
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
    }
}
