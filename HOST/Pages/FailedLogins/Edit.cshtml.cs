using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages.FailedLogins
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
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var existing = await _context.FailedLogins.FirstOrDefaultAsync(f => f.Id == FailedLogin.Id);
            if (existing == null)
            {
                return NotFound();
            }

            existing.Username = FailedLogin.Username;
            existing.Timestamp = FailedLogin.Timestamp;
            existing.IpAddress = FailedLogin.IpAddress;

            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
