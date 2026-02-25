using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages.FailedLogins
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
        public FailedLogin FailedLogin { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var entry = await _context.FailedLogins.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);
            if (entry == null)
            {
                return NotFound();
            }

            FailedLogin = entry;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var entry = await _context.FailedLogins.FindAsync(FailedLogin.Id);
            if (entry == null)
            {
                return NotFound();
            }

            _context.FailedLogins.Remove(entry);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
