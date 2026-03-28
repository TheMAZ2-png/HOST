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
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TableId == id);

            if (Table == null)
                return NotFound();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            // Load table with tracking
            var table = await _context.RestaurantTables
                .FirstOrDefaultAsync(t => t.TableId == id);

            if (table == null)
                return NotFound();

            if (table.CurrentPartyId == null)
            {
                TempData["ErrorMessage"] = "This table is already clear.";
                return RedirectToPage("Index");
            }

            // Load the party assigned to this table
            var party = await _context.Parties
                .FirstOrDefaultAsync(p => p.PartyId == table.CurrentPartyId);

            if (party != null)
            {
                // Update party status
                party.Status = "Completed";

                // OPTIONAL: delete the party entirely
                // _context.Parties.Remove(party);
            }

            // Clear the table
            table.Status = "Available";
            table.CurrentPartyId = null;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Table cleared successfully.";
            return RedirectToPage("Index");
        }
    }
}
