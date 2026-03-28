using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HOST.Pages.Parties
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Party> WaitingParties { get; set; } = new List<Party>();
        public IList<Party> SeatedParties { get; set; } = new List<Party>();
        public IList<Party> CompletedParties { get; set; } = new List<Party>();

        public async Task OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (User.IsInRole("Manager") || User.IsInRole("Host") || User.IsInRole("Server"))
            {
                // STAFF: See all parties by status
                WaitingParties = await _context.Parties
                    .Where(p => p.Status == "Waiting")
                    .Include(p => p.QueueEntries)
                    .OrderBy(p => p.CreatedAt)
                    .ToListAsync();

                SeatedParties = await _context.Parties
                    .Where(p => p.Status == "Seated")
                    .Include(p => p.Seatings)
                    .OrderBy(p => p.CreatedAt)
                    .ToListAsync();

                CompletedParties = await _context.Parties
                    .Where(p => p.Status == "Completed")
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();
            }
            else
            {
                // GUEST: Only see their own Waiting party
                WaitingParties = await _context.Parties
                    .Where(p => p.OwnerId == userId && p.Status == "Waiting")
                    .Include(p => p.QueueEntries)
                    .OrderBy(p => p.CreatedAt)
                    .ToListAsync();
            }
        }
    }
}
