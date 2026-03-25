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

            // ⭐ NEW BEHAVIOR:
            // Party + QueueEntry are already deleted during seating.
            // Clearing the table ONLY resets the table.

            table.Status = "Available";
            table.CurrentPartyId = null;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Table cleared successfully.";
            return RedirectToPage("Index");
        }
    }
}
