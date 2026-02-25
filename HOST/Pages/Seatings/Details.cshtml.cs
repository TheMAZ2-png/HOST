using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages.Seatings
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Seating Seating { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var seating = await _context.Seatings.AsNoTracking().FirstOrDefaultAsync(s => s.SeatingId == id);
            if (seating == null)
            {
                return NotFound();
            }

            Seating = seating;
            return Page();
        }
    }
}
