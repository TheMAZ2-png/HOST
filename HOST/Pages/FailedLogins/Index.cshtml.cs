using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages.FailedLogins
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<FailedLogin> FailedLogins { get; set; } = new List<FailedLogin>();

        public async Task OnGetAsync()
        {
            FailedLogins = await _context.FailedLogins.AsNoTracking().ToListAsync();
        }
    }
}
