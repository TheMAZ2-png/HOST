using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages.QueueEntries
{
    [Authorize]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
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
            var entry = await _context.QueueEntries.FindAsync(QueueEntry.QueueEntryId);
            if (entry == null)
            {
                return NotFound();
            }

            _context.QueueEntries.Remove(entry);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
