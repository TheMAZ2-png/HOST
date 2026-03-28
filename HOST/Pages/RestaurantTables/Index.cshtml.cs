using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages.RestaurantTables
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<RestaurantTable> RestaurantTables { get; set; } = new List<RestaurantTable>();

        public async Task OnGetAsync()
        {
            RestaurantTables = await _context.RestaurantTables
                .Include(t => t.CurrentParty)
                .AsNoTracking()
                .OrderBy(t => t.TableNumber)
                .ToListAsync();
        }
    }
}
