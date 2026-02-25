using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

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

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            QueueEntry.CreatedAt = DateTime.UtcNow;
            QueueEntry.UpdatedAt = null;

            _context.QueueEntries.Add(QueueEntry);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
