using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages.QueueEntries
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public QueueEntry QueueEntry { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var entry = await _context.QueueEntries.AsNoTracking().FirstOrDefaultAsync(q => q.QueueEntryId == id);
            if (entry == null)
            {
                return NotFound();
            }

            QueueEntry = entry;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var existing = await _context.QueueEntries.FirstOrDefaultAsync(q => q.QueueEntryId == QueueEntry.QueueEntryId);
            if (existing == null)
            {
                return NotFound();
            }

            existing.PartyId = QueueEntry.PartyId;
            existing.Notes = QueueEntry.Notes;
            existing.PublicAccessCode = QueueEntry.PublicAccessCode;
            existing.Status = QueueEntry.Status;
            existing.EstimatedWaitMinutes = QueueEntry.EstimatedWaitMinutes;
            existing.SeatedAt = QueueEntry.SeatedAt;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
