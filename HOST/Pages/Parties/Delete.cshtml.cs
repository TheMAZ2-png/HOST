using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages.Parties
{
    [Authorize(Roles = "Manager")]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Party Party { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            Party = await _context.Parties
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PartyId == id);

            if (Party == null)
                return NotFound();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var party = await _context.Parties
                .FirstOrDefaultAsync(p => p.PartyId == Party.PartyId);

            if (party == null)
                return NotFound();

            // ❌ Prevent deleting Seated or Completed parties
            if (party.Status != "Waiting")
            {
                TempData["ErrorMessage"] = "Only parties that are still waiting can be deleted.";
                return RedirectToPage("./Index");
            }

            // Delete queue entries first
            var queueEntries = _context.QueueEntries.Where(q => q.PartyId == party.PartyId);
            _context.QueueEntries.RemoveRange(queueEntries);

            // Delete the party
            _context.Parties.Remove(party);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Party deleted successfully.";
            return RedirectToPage("./Index");
        }
    }
}
