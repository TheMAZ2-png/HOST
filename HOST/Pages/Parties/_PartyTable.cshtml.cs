using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages.Parties
{
    public class _PartyTableModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public _PartyTableModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // ⭐ This is the property your .cshtml loops over
        public IList<Party> Parties { get; set; } = new List<Party>();

        public async Task OnGetAsync()
        {
            Parties = await _context.Parties
                .Where(p => !p.IsDeleted)
                .OrderBy(p => p.CreatedAt)
                .ToListAsync();

            // Calculate live wait time
            foreach (var party in Parties)
            {
                party.ActualWaitMinutes =
                    (int)Math.Floor((DateTime.UtcNow - party.CreatedAt).TotalMinutes);
            }
        }
    }
}
