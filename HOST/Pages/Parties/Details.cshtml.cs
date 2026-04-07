using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages.Parties
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Party Party { get; set; } = new();
        public RestaurantTable? CurrentTable { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            Party = await _context.Parties
                .Include(p => p.QueueEntries)
                .Include(p => p.Seatings)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PartyId == id);

            if (Party == null)
                return NotFound();

            // If seated, load the most recent seating record
            if (Party.Status == "Seated")
            {
                var seating = Party.Seatings
                    .OrderByDescending(s => s.SeatedAt)   // FIXED
                    .FirstOrDefault();

                if (seating != null)
                {
                    CurrentTable = await _context.RestaurantTables
                        .AsNoTracking()
                        .FirstOrDefaultAsync(t => t.TableId == seating.RestaurantTableId); // FIXED
                }
            }

            return Page();
        }
    }
}
