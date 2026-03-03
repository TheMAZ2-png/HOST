using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages.Seatings
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Seating> Seatings { get; set; } = new List<Seating>();

        public async Task OnGetAsync()
        {
            Seatings = await _context.Seatings.AsNoTracking().ToListAsync();
        }
    }
}
