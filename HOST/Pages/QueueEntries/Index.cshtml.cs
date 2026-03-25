using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages.QueueEntries
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<QueueEntry> QueueEntries { get; set; } = new List<QueueEntry>();

        public async Task OnGetAsync()
        {
            QueueEntries = await _context.QueueEntries
                .Where(q => q.Status == "Waiting")   // ⭐ Only show waiting parties
                .Include(q => q.Party)               // Load party info
                .AsNoTracking()
                .OrderBy(q => q.CreatedAt)
                .ToListAsync();
        }
    }
}
