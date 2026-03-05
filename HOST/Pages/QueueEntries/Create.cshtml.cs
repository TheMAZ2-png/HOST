using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HOST.Pages.QueueEntries
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public QueueEntry QueueEntry { get; set; } = new();

        public List<SelectListItem> PartyOptions { get; set; }

        public IActionResult OnGet()
        {
            LoadPartyDropdown();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            QueueEntry.CreatedAt = DateTime.UtcNow;

            if (!ModelState.IsValid)
            {
                LoadPartyDropdown();
                return Page();
            }

            _context.QueueEntries.Add(QueueEntry);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

        private void LoadPartyDropdown()
        {
            var activePartyIds = _context.QueueEntries
                .Where(q => q.Status == "Waiting")
                .Select(q => q.PartyId)
                .ToHashSet();

            PartyOptions = _context.Parties
                .Where(p => !activePartyIds.Contains(p.PartyId))
                .Select(p => new SelectListItem
                {
                    Value = p.PartyId.ToString(),
                    Text = $"{p.PartyName} (Size: {p.PartySize})"
                })
                .ToList();
        }
    }
}
