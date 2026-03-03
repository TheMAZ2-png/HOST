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
            var table = await _context.RestaurantTables.FindAsync(RestaurantTable.TableId);
            if (table == null)
            {
                return NotFound();
            }

            _context.RestaurantTables.Remove(table);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
