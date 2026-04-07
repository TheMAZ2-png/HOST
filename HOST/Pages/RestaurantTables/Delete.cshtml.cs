using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages.RestaurantTables
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
        public RestaurantTable RestaurantTable { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            RestaurantTable = await _context.RestaurantTables
                .Include(t => t.CurrentParty)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TableId == id);

            if (RestaurantTable == null)
                return NotFound();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var table = await _context.RestaurantTables
                .FirstOrDefaultAsync(t => t.TableId == RestaurantTable.TableId);

            if (table == null)
                return NotFound();

            // ❌ Prevent deletion if a party is seated
            if (table.CurrentPartyId != null)
            {
                TempData["ErrorMessage"] = "Cannot delete a table that is currently occupied.";
                return RedirectToPage("./Index");
            }

            _context.RestaurantTables.Remove(table);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Table deleted successfully.";
            return RedirectToPage("./Index");
        }
    }
}
