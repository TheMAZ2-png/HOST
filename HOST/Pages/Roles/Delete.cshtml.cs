using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages.Roles
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
        public Role Role { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var role = await _context.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.RoleId == id);
            if (role == null)
            {
                return NotFound();
            }

            Role = role;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var role = await _context.Roles.FindAsync(Role.RoleId);
            if (role == null)
            {
                return NotFound();
            }

            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
