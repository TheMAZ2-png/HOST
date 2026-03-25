using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace HOST.Pages.Parties
{
    [Authorize(Roles = "Manager")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Party Party { get; set; } = new();

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

            // ⭐ FIX A — Set OwnerId to the logged‑in Manager’s IdentityUser ID
            Party.OwnerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 1. Create the Party
            _context.Parties.Add(Party);
            await _context.SaveChangesAsync();

            // 2. Automatically create a QueueEntry
            var queueEntry = new QueueEntry
            {
                PartyId = Party.PartyId,
                Status = "Waiting",
                CreatedAt = DateTime.UtcNow
            };

            _context.QueueEntries.Add(queueEntry);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
