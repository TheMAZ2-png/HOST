using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HOST.Pages.Parties
{
    [AllowAnonymous]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Party> Parties { get; set; } = new List<Party>();

        public async Task OnGetAsync()
        {
            // Load all parties for everyone
            Parties = await _context.Parties
                .AsNoTracking()
                .ToListAsync();
        }

        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> OnPostDeleteAllAsync()
        {
            _context.Parties.RemoveRange(_context.Parties);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}
