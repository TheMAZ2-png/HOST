using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages.Seatings
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

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var existing = await _context.Seatings.FirstOrDefaultAsync(s => s.SeatingId == Seating.SeatingId);
            if (existing == null)
            {
                return NotFound();
            }

            existing.AssignedServerId = Seating.AssignedServerId;
            existing.SeatedByEmployeeId = Seating.SeatedByEmployeeId;
            existing.RestaurantTableId = Seating.RestaurantTableId;
            existing.PartyId = Seating.PartyId;
            existing.SeatedAt = Seating.SeatedAt;
            existing.ClearedAt = Seating.ClearedAt;

            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
