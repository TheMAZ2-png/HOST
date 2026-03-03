using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages.ManagerAccounts
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<ManagerAccount> ManagerAccounts { get; set; } = new List<ManagerAccount>();

        public async Task OnGetAsync()
        {
            ManagerAccounts = await _context.ManagerAccounts.AsNoTracking().ToListAsync();
        }
    }
}
