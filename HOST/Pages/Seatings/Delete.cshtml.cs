using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages.Seatings
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
            var seating = await _context.Seatings.FindAsync(Seating.SeatingId);
            if (seating == null)
            {
                return NotFound();
            }

            _context.Seatings.Remove(seating);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
