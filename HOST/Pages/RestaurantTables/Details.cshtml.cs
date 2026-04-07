using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages.RestaurantTables
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public RestaurantTable RestaurantTable { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            RestaurantTable = await _context.RestaurantTables
                .Include(t => t.CurrentParty)
                .Include(t => t.Seatings)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TableId == id);

            if (RestaurantTable == null)
                return NotFound();

            return Page();
        }
    }
}
