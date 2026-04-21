using System.Net.Http;
using System.Text;
using System.Text.Json;
using HOST.Models;
using Microsoft.Extensions.Configuration;

namespace HOST.Services
{
    public class AIService
    {
        private readonly string _apiKey;
        private readonly string _model;
        private readonly HttpClient _http;

        public AIService(IConfiguration config, HttpClient http)
        {
            _apiKey = config["AISettings:ApiKey"];
            _model = config["AISettings:Model"];
            _http = http;
        }

        public async Task<string> SendGeneralPromptAsync(string prompt)
        {
            return await CallGeminiAsync(prompt);
        }

        public async Task<string> SendPromptWithMenuListAsync(List<MenuItem> menuItems, string prompt)
        {
            var menuJson = JsonSerializer.Serialize(menuItems);
            var fullPrompt = $"{prompt}\n\nMenu JSON:\n{menuJson}";
            return await CallGeminiAsync(fullPrompt);
        }

        // ⭐ Strong PDF → JSON parser
        public async Task<string> DigestPdfToMenuJsonAsync(string extractedText)
        {
            var instruction =
                "You are a professional menu parser. Your job is to read messy, unstructured PDF text " +
                "and convert it into a structured JSON object with this exact shape:\n\n" +
                "{\n" +
                "  \"menu_name\": string,\n" +
                "  \"date\": string,\n" +
                "  \"chef\": string,\n" +
                "  \"categories\": [\n" +
                "    {\n" +
                "      \"category_name\": string,\n" +
                "      \"items\": [\n" +
                "        {\n" +
                "          \"name\": string,\n" +
                "          \"description\": string,\n" +
                "          \"calories\": number,\n" +
                "          \"price\": number\n" +
                "        }\n" +
                "      ]\n" +
                "    }\n" +
                "  ]\n" +
                "}\n\n" +
                "Rules:\n" +
                "- ALWAYS output valid JSON.\n" +
                "- Infer categories when missing.\n" +
                "- Merge multi-line descriptions.\n" +
                "- Extract prices even if separated from item names.\n" +
                "- If calories or price are missing, set them to 0.\n" +
                "- Ignore page numbers, headers, and footers.\n" +
                "- Do not include any text outside the JSON.\n\n" +
                "Now parse the following text:\n\n" +
                extractedText;

            return await CallGeminiAsync(instruction);
        }

        // Overload with menu name context
        public async Task<string> DigestPdfToMenuJsonAsync(string extractedText, string menuName)
        {
            var combined =
                $"Menu Name: {menuName}\n\n" +
                "The following text comes from a PDF. Parse it into the structured menu JSON format.\n\n" +
                extractedText;

            return await DigestPdfToMenuJsonAsync(combined);
        }

        public Task<List<string>> ListAvailableModelsAsync()
        {
            return Task.FromResult(new List<string> { _model });
        }

        private async Task<string> CallGeminiAsync(string prompt)
        {
            var url =
                $"https://generativelanguage.googleapis.com/v1beta/{_model}:generateContent?key={_apiKey}";

            var requestBody = new
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

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync(url, content);
            var responseJson = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(responseJson);

            try
            {
                return doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? "No response text.";
            }
            catch
            {
                return responseJson;
            }
        }
    }
}
