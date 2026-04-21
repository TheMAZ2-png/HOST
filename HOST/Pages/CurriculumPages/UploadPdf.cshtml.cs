using HOST.Models;
using HOST.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace HOST.Pages.CurriculumPages
{
    [Authorize(Roles = "Manager")]
    public class UploadPdfModel : PageModel
    {
        private readonly MongoDBService _mongo;
        private readonly AIService _ai;
        private readonly ILogger<UploadPdfModel> _logger;

        public UploadPdfModel(MongoDBService mongo, AIService ai, ILogger<UploadPdfModel> logger)
        {
            _mongo = mongo;
            _ai = ai;
            _logger = logger;
        }

        [BindProperty]
        public IFormFile? PdfFile { get; set; }

        [BindProperty]
        public string MenuName { get; set; } = string.Empty;

        public string? ExtractedText { get; set; }
        public string? AiJsonResult { get; set; }
        public string? SavedMessage { get; set; }
        public bool IsProcessing { get; set; }

        public void OnGet() { }

        // STEP 1 — Upload + Extract + AI Digest
        public async Task<IActionResult> OnPostUploadAsync()
        {
            IsProcessing = true;

            try
            {
                if (string.IsNullOrWhiteSpace(MenuName))
                {
                    ModelState.AddModelError(string.Empty, "Please provide a name for the menu.");
                    return Page();
                }

                if (PdfFile == null || PdfFile.Length == 0)
                {
                    ModelState.AddModelError(string.Empty, "Please select a PDF file.");
                    return Page();
                }

                if (!PdfFile.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(string.Empty, "Only PDF files are allowed.");
                    return Page();
                }

                // Extract text
                var sb = new StringBuilder();
                using (var stream = PdfFile.OpenReadStream())
                using (var pdfDocument = UglyToad.PdfPig.PdfDocument.Open(stream)) // ⭐ FIXED
                {
                    foreach (var page in pdfDocument.GetPages())
                    {
                        sb.AppendLine(page.Text);
                    }
                }

                ExtractedText = sb.ToString();

                if (string.IsNullOrWhiteSpace(ExtractedText))
                {
                    ModelState.AddModelError(string.Empty, "Could not extract text from the PDF.");
                    return Page();
                }

                // Send to AI
                AiJsonResult = await _ai.DigestPdfToMenuJsonAsync(ExtractedText, MenuName);

                // Store for Save step
                TempData["ExtractedText"] = ExtractedText;
                TempData["AiJsonResult"] = AiJsonResult;
                TempData["FileName"] = PdfFile.FileName;
                TempData["MenuName"] = MenuName;

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PDF upload/digest failed.");
                ModelState.AddModelError(string.Empty, "Failed to process PDF: " + ex.Message);
                return Page();
            }
            finally
            {
                IsProcessing = false;
            }
        }

        // STEP 2 — Save to MongoDB
        public async Task<IActionResult> OnPostSaveAsync()
        {
            try
            {
                var extractedText = TempData["ExtractedText"]?.ToString() ?? "";
                var aiJsonResult = TempData["AiJsonResult"]?.ToString() ?? "";
                var fileName = TempData["FileName"]?.ToString() ?? "unknown.pdf";
                var menuName = TempData["MenuName"]?.ToString() ?? "";

                if (string.IsNullOrWhiteSpace(aiJsonResult))
                {
                    ModelState.AddModelError(string.Empty, "No AI result to save.");
                    return Page();
                }

                curriculum? parsedCurriculum = null;

                try
                {
                    var cleanedJson = aiJsonResult
                        .Replace("```json", "")
                        .Replace("```", "")
                        .Trim();

                    // Try full curriculum first
                    parsedCurriculum = JsonSerializer.Deserialize<curriculum>(cleanedJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    // If null, try flat list → convert to curriculum
                    if (parsedCurriculum == null)
                    {
                        var flatItems = JsonSerializer.Deserialize<List<MenuItem>>(cleanedJson,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (flatItems != null)
                        {
                            parsedCurriculum = new curriculum
                            {
                                Id = "SPECIALS_MENU",
                                menu_name = menuName,
                                date = "",
                                chef = "",
                                categories = flatItems
                                    .GroupBy(i => i.category)
                                    .Select(g => new MenuCategory
                                    {
                                        category_name = g.Key,
                                        items = g.ToList()
                                    })
                                    .ToList()
                            };
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "AI result could not be parsed.");
                }

                // Save PDF record
                var pdfDoc = new HOST.Models.PdfDocument
                {
                    FileName = fileName,
                    UploadedAt = DateTime.UtcNow,
                    ExtractedText = extractedText,
                    AiDigestedJson = aiJsonResult,
                    ParsedCurriculum = parsedCurriculum
                };
                await _mongo.CreatePdfDocumentAsync(pdfDoc);

                // Save curriculum if parsed
                if (parsedCurriculum != null)
                {
                    parsedCurriculum.Id = "SPECIALS_MENU";
                    await _mongo.ReplaceMenuAsync(parsedCurriculum);
                }

                SavedMessage = $"Successfully saved menu \"{menuName}\" to MongoDB!";
                AiJsonResult = aiJsonResult;
                ExtractedText = extractedText;
                MenuName = menuName;

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save to MongoDB.");
                ModelState.AddModelError(string.Empty, "Failed to save: " + ex.Message);
                return Page();
            }
        }
    }
}
