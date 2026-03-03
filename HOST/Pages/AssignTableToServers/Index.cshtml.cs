using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages.AssignTableToServers
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<AssignTableToServer> Assignments { get; set; } = new List<AssignTableToServer>();

        public async Task OnGetAsync()
        {
            Assignments = await _context.AssignTableToServers.AsNoTracking().ToListAsync();
        }
    }
}
