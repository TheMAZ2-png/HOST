using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages.RestaurantTables
{
    [Authorize]
    public class ClearModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ClearModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public RestaurantTable Table { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Table = await _context.RestaurantTables
                .Include(t => t.CurrentParty)
                .FirstOrDefaultAsync(t => t.TableId == id);

            if (Table == null)
                return NotFound();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var table = await _context.RestaurantTables
                .FirstOrDefaultAsync(t => t.TableId == id);

            if (table == null)
                return NotFound();

            if (table.CurrentPartyId == null)
            {
                TempData["ErrorMessage"] = "This table is already clear.";
                return RedirectToPage("Index");
            }

            int partyId = table.CurrentPartyId.Value;

            // Load Party
            var party = await _context.Parties
                .Include(p => p.QueueEntries)
                .Include(p => p.Seatings)
                .FirstOrDefaultAsync(p => p.PartyId == partyId);

            if (party == null)
            {
                // Fallback: clear table anyway
                table.Status = "Available";
                table.CurrentPartyId = null;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Table cleared.";
                return RedirectToPage("Index");
            }

            // ⭐ 1. Clear the table
            table.Status = "Available";
            table.CurrentPartyId = null;

            // ⭐ 2. Delete all Seatings for this Party
            var seatings = await _context.Seatings
                .Where(s => s.PartyId == partyId)
                .ToListAsync();

            _context.Seatings.RemoveRange(seatings);

            // ⭐ 3. Delete all QueueEntries for this Party
            var queueEntries = await _context.QueueEntries
                .Where(q => q.PartyId == partyId)
                .ToListAsync();

            _context.QueueEntries.RemoveRange(queueEntries);

            // ⭐ 4. Delete the Party itself
            _context.Parties.Remove(party);

            // ⭐ 5. Save all changes
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Table cleared and party removed.";
            return RedirectToPage("Index");
        }
    }
}
