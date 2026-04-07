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
            bool isStaff = User.IsInRole("Manager") ||
                           User.IsInRole("Host") ||
                           User.IsInRole("Server");

            if (isStaff)
            {
                QueueEntries = await _context.QueueEntries
                    .Where(q => q.Status == "Waiting")
                    .Include(q => q.Party)
                    .AsNoTracking()
                    .OrderBy(q => q.CreatedAt)
                    .ToListAsync();
            }
            else
            {
                QueueEntries = new List<QueueEntry>();
            }
        }
    }
}
