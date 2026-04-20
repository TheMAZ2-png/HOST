using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

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
        public IList<Party> DeletedParties { get; set; } = new List<Party>();

        public async Task OnGetAsync()
        {
            bool isManager = User.IsInRole("Manager");

            WaitingParties = await _context.Parties
                .Where(p => !p.IsDeleted && p.Status == "Waiting")
                .OrderBy(p => p.CreatedAt)
                .ToListAsync();

            SeatedParties = await _context.Parties
                .Where(p => !p.IsDeleted && p.Status == "Seated")
                .OrderBy(p => p.CreatedAt)
                .ToListAsync();

            CompletedParties = await _context.Parties
                .Where(p => !p.IsDeleted && p.Status == "Completed")
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            if (isManager)
            {
                DeletedParties = await _context.Parties
                    .Where(p => p.IsDeleted)
                    .OrderByDescending(p => p.DeletedAt)
                    .ToListAsync();
            }

            foreach (var party in WaitingParties)
            {
                party.ActualWaitMinutes =
                    (int)Math.Floor((DateTime.UtcNow - party.CreatedAt).TotalMinutes);
            }

            var orderedWaiting = WaitingParties.OrderBy(p => p.CreatedAt).ToList();
            for (int i = 0; i < orderedWaiting.Count; i++)
            {
                var party = orderedWaiting[i];
                party.EstimatedWaitMinutes = await CalculateEstimatedWaitAsync(party.PartySize, i);
            }
        }

        public async Task<IActionResult> OnPostRestoreAsync(int id)
        {
            var party = await _context.Parties.FindAsync(id);
            if (party == null)
                return NotFound();

            party.IsDeleted = false;
            party.DeletedAt = null;

            await _context.SaveChangesAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAllAsync()
        {
            var parties = await _context.Parties.ToListAsync();
            foreach (var party in parties)
            {
                party.IsDeleted = true;
                party.DeletedAt = DateTime.UtcNow;
            }

            var queueEntries = await _context.QueueEntries.ToListAsync();
            foreach (var entry in queueEntries)
            {
                entry.Status = "Deleted";
                entry.UpdatedAt = DateTime.UtcNow;
            }

            var tables = await _context.RestaurantTables.ToListAsync();
            foreach (var table in tables)
            {
                table.Status = "Available";
                table.CurrentPartyId = null;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "All parties have been soft-deleted and moved to the Deleted Parties section.";
            return RedirectToPage();
        }

        private async Task<int> CalculateEstimatedWaitAsync(int partySize, int position)
        {
            var recentSeatings = await _context.Seatings
                .Where(s => s.SeatedAt > DateTime.UtcNow.AddHours(-2))
                .Include(s => s.Party)
                .OrderByDescending(s => s.SeatedAt)
                .Take(20)
                .ToListAsync();

            double avgWait = recentSeatings
                .Where(s => s.Party.ActualWaitMinutes.HasValue)
                .Select(s => (double)s.Party.ActualWaitMinutes.Value)
                .DefaultIfEmpty(10)
                .Average();

            int availableTables = await _context.RestaurantTables
                .CountAsync(t => t.Status == "Available" && t.CurrentPartyId == null);

            double tableWeight = availableTables == 0 ? 1.5 : 1.0;
            double sizeWeight = Math.Max(1.0, 1.0 + (partySize - 2) * 0.10);

            double estimate = (position + 1) * avgWait * sizeWeight * tableWeight;

            estimate = Math.Clamp(estimate, 5, 90);

            return (int)Math.Round(estimate);
        }
    }
}
