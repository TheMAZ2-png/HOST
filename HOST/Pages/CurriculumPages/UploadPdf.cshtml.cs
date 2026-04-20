using HOST.Models;
using HOST.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using System.Text.Json;
using UglyToad.PdfPig;
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

        // This is now the correct property name for the menu
        [BindProperty]
        public string MenuName { get; set; } = string.Empty;

        public string? ExtractedText { get; set; }
        public string? AiJsonResult { get; set; }
        public string? SavedMessage { get; set; }
        public bool IsProcessing { get; set; }

        public void OnGet()
        {
        }

        /// <summary>
        /// Step 1: Upload PDF, extract text, send to AI with the user-provided menu name.
        /// </summary>
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

                // Extract text from PDF
                var sb = new StringBuilder();
                using (var stream = PdfFile.OpenReadStream())
                using (var pdfDocument = UglyToad.PdfPig.PdfDocument.Open(stream))
                {
                    foreach (var page in pdfDocument.GetPages())
                    {
                        sb.AppendLine(page.Text);
                    }
                }

                ExtractedText = sb.ToString();

                if (string.IsNullOrWhiteSpace(ExtractedText))
                {
                    ModelState.AddModelError(string.Empty, "Could not extract text from the PDF. It may be image-based.");
                    return Page();
                }

                // Send to Gemini AI with the user-provided menu name
                AiJsonResult = await _ai.DigestPdfToMenuJsonAsync(ExtractedText, MenuName);

                // Store in TempData for the Save step
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

        /// <summary>
        /// Step 2: Save the AI-digested JSON to MongoDB.
        /// </summary>
        public async Task<IActionResult> OnPostSaveAsync()
        {
            try
            {
                var extractedText = TempData["ExtractedText"]?.ToString() ?? string.Empty;
                var aiJsonResult = TempData["AiJsonResult"]?.ToString() ?? string.Empty;
                var fileName = TempData["FileName"]?.ToString() ?? "unknown.pdf";
                var menuName = TempData["MenuName"]?.ToString() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(aiJsonResult))
                {
                    ModelState.AddModelError(string.Empty, "No AI result to save. Please upload a PDF first.");
                    return Page();
                }

                // Try to parse the AI JSON into a curriculum (menu) object
                curriculum? parsedCurriculum = null;
                try
                {
                    // Clean AI JSON (remove backticks and code fences)
                    var cleanedJson = aiJsonResult
                        .Replace("```json", "")
                        .Replace("```", "")
                        .Trim();

                    parsedCurriculum = JsonSerializer.Deserialize<curriculum>(cleanedJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });


                    // Ensure the user-provided menu name is used
                    if (parsedCurriculum != null && !string.IsNullOrWhiteSpace(menuName))
                    {
                        parsedCurriculum.menu_name = menuName;
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "AI result could not be parsed as a menu object. Saving raw JSON only.");
                }

                // Save the PDF document record
                var pdfDoc = new Models.PdfDocument
                {
                    FileName = fileName,
                    UploadedAt = DateTime.UtcNow,
                    ExtractedText = extractedText,
                    AiDigestedJson = aiJsonResult,
                    ParsedCurriculum = parsedCurriculum
                };
                await _mongo.CreatePdfDocumentAsync(pdfDoc);

                // If parsed successfully, also save to the menu/curriculum collection
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
