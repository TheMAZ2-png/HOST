using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages.RestaurantTables
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
            if (!ModelState.IsValid)
                return Page();

            var existing = await _context.RestaurantTables
                .Include(t => t.CurrentParty)
                .FirstOrDefaultAsync(t => t.TableId == RestaurantTable.TableId);

            if (existing == null)
                return NotFound();

            // ❌ Prevent editing if a party is seated
            if (existing.CurrentPartyId != null)
            {
                TempData["ErrorMessage"] = "Cannot edit a table that is currently occupied.";
                return RedirectToPage("./Index");
            }

            // Editable fields
            existing.TableNumber = RestaurantTable.TableNumber;
            existing.SeatCapacity = RestaurantTable.SeatCapacity;
            existing.Section = RestaurantTable.Section;
            existing.IsActive = RestaurantTable.IsActive;

            // ❌ Status is system-controlled (Seat/Clear)
            // Do NOT update existing.Status

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Table updated successfully.";
            return RedirectToPage("./Index");
        }
    }
}
