using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HOST.Pages.Seatings
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Seating Seating { get; set; } = new();

        public IActionResult OnGet()
        {
            if (Seating.SeatedAt == default)
            {
                Seating.SeatedAt = DateTime.UtcNow;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Seatings.Add(Seating);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
