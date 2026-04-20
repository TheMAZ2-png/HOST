using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
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
        private readonly string _fallbackModel = "gemini-1.5-flash";

        public AIService(HttpClient httpClient, IConfiguration config, ILogger<AIService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            var aiSettings = config.GetSection("AISettings");
            _apiKey = aiSettings["ApiKey"];
            _model = aiSettings["Model"] ?? "gemini-2.5-flash";

            var baseAddress = aiSettings["BaseAddress"] ?? "https://generativelanguage.googleapis.com/v1beta/";
            _httpClient.BaseAddress = new Uri(baseAddress);
        }

        // ---------------------------------------------------------
        // ⭐ Core Gemini Call (API key in query string)
        // ---------------------------------------------------------
        private async Task<string> CallGeminiAsync(string model, object payload)
        {
            var json = JsonSerializer.Serialize(payload);
            var url = $"models/{model}:generateContent?key={_apiKey}";

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            // ❗ DO NOT SET Authorization header for API key
            // request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            using var resp = await _httpClient.SendAsync(request);
            var respText = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini API error: {Status} - {Response}", resp.StatusCode, respText);
                throw new InvalidOperationException($"Gemini API error: {resp.StatusCode} - {respText}");
            }

            return ExtractGeminiText(respText, _logger);
        }

        // ---------------------------------------------------------
        // ⭐ Retry + fallback wrapper
        // ---------------------------------------------------------
        private async Task<string> CallGeminiWithRetryAsync(string model, object payload)
        {
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    return await CallGeminiAsync(model, payload);
                }
                catch (Exception ex)
                {
                    if (attempt == 3)
                        break;

                    _logger.LogWarning("Gemini call failed (attempt {Attempt}): {Message}", attempt, ex.Message);
                    await Task.Delay(500 * attempt);
                }
            }

            _logger.LogWarning("Falling back to model: {Fallback}", _fallbackModel);
            return await CallGeminiAsync(_fallbackModel, payload);
        }

        // ---------------------------------------------------------
        // ⭐ Extract text from Gemini response
        // ---------------------------------------------------------
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
                        return parts[0].GetProperty("text").GetString() ?? "";
                    }
                }

                logger.LogError("Unexpected Gemini JSON: {Response}", respText);
                return "";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to parse Gemini JSON: {Response}", respText);
                throw;
            }
        }

        // ---------------------------------------------------------
        // ⭐ Public API Methods
        // ---------------------------------------------------------
        public async Task<string> SendGeneralPromptAsync(string userMessage)
        {
            var payload = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = userMessage } } }
                }
            };

            return await CallGeminiWithRetryAsync(_model, payload);
        }

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

        public async Task<string> SendCurriculumAsync(curriculum cur)
        {
            var prompt = $"Analyze this curriculum JSON:\n{JsonSerializer.Serialize(cur)}";

            return await AskGeminiAsync(prompt);
        }

        public async Task<string> SendPromptWithCurriculumAsync(curriculum cur, string userQuestion)
        {
            var prompt = $"{userQuestion}\n\nCurriculum:\n{JsonSerializer.Serialize(cur)}";

            return await AskGeminiAsync(prompt);
        }

        public async Task<string> DigestPdfToMenuJsonAsync(string pdfText, string menuName)
        {
            var prompt = $"Convert this menu text into JSON:\n{pdfText}";
            return await AskGeminiAsync(prompt);
        }

        public async Task<string> SendPromptWithMenuItemAsync(MenuItem item, string userQuestion)
        {
            var prompt = $"{userQuestion}\n\nMenu Item:\n{JsonSerializer.Serialize(item)}";
            return await AskGeminiAsync(prompt);
        }

        public async Task<string> SendPromptWithMenuListAsync(List<MenuItem> items, string userQuestion)
        {
            var prompt = $"{userQuestion}\n\nMenu:\n{JsonSerializer.Serialize(items)}";
            return await AskGeminiAsync(prompt);
        }
    }
}
