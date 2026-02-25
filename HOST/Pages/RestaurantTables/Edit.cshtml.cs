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
            {
                return NotFound();
            }

            var table = await _context.RestaurantTables.AsNoTracking().FirstOrDefaultAsync(t => t.TableId == id);
            if (table == null)
            {
                return NotFound();
            }

            RestaurantTable = table;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var existing = await _context.RestaurantTables.FirstOrDefaultAsync(t => t.TableId == RestaurantTable.TableId);
            if (existing == null)
            {
                return NotFound();
            }

            existing.TableNumber = RestaurantTable.TableNumber;
            existing.SeatCapacity = RestaurantTable.SeatCapacity;
            existing.Section = RestaurantTable.Section;
            existing.Status = RestaurantTable.Status;
            existing.IsActive = RestaurantTable.IsActive;

            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
