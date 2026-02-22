using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

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

        public int? Id { get; set; }
        public IList<Party> Parties { get; set; } = new List<Party>();

        public async Task OnGetAsync(int? id)
        {
            Id = id;

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
