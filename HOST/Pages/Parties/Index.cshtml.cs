using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        // ============================================================
        // GET — Load all parties
        // ============================================================
        public async Task OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // STAFF VIEW
            if (User.IsInRole("Manager") || User.IsInRole("Host") || User.IsInRole("Server"))
            {
                WaitingParties = await _context.Parties
                    .Where(p => p.Status == "Waiting")
                    .OrderBy(p => p.CreatedAt)
                    .ToListAsync();

                SeatedParties = await _context.Parties
                    .Where(p => p.Status == "Seated")
                    .OrderBy(p => p.CreatedAt)
                    .ToListAsync();

                CompletedParties = await _context.Parties
                    .Where(p => p.Status == "Completed")
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();
            }
            else
            {
                // GUEST VIEW
                WaitingParties = await _context.Parties
                    .Where(p => p.Status == "Waiting")
                    .OrderBy(p => p.CreatedAt)
                    .ToListAsync();
            }

            // ============================================================
            // ⭐ LIVE WAIT TIME FOR WAITING PARTIES
            // ============================================================
            foreach (var party in WaitingParties)
            {
                party.ActualWaitMinutes =
                    (int)Math.Floor((DateTime.UtcNow - party.CreatedAt).TotalMinutes);
            }

            // ============================================================
            // ⭐ ESTIMATED WAIT-TIME ALGORITHM
            // ============================================================
            var orderedWaiting = WaitingParties
                .OrderBy(p => p.CreatedAt)
                .ToList();

            for (int i = 0; i < orderedWaiting.Count; i++)
            {
                var party = orderedWaiting[i];
                party.EstimatedWaitMinutes = await CalculateEstimatedWaitAsync(party.PartySize, i);
            }
        }

        // ============================================================
        // ⭐ POST — HARD DELETE ALL PARTIES + RELATED DATA
        // ============================================================
        public async Task<IActionResult> OnPostDeleteAllAsync()
        {
            // 1. Delete all queue entries
            var queueEntries = await _context.QueueEntries.ToListAsync();
            _context.QueueEntries.RemoveRange(queueEntries);

            // 2. Delete all seatings
            var seatings = await _context.Seatings.ToListAsync();
            _context.Seatings.RemoveRange(seatings);

            // 3. Reset all tables
            var tables = await _context.RestaurantTables.ToListAsync();
            foreach (var table in tables)
            {
                table.Status = "Available";
                table.CurrentPartyId = null;
            }

            // 4. Delete all parties
            var parties = await _context.Parties.ToListAsync();
            _context.Parties.RemoveRange(parties);

            // 5. Save changes
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "All parties and related data have been permanently deleted.";
            return RedirectToPage();
        }

        // ============================================================
        // ⭐ ESTIMATED WAIT-TIME ALGORITHM
        // ============================================================
        private async Task<int> CalculateEstimatedWaitAsync(int partySize, int position)
        {
            // 1. Recent seatings (last 2 hours)
            var recentSeatings = await _context.Seatings
                .Where(s => s.SeatedAt > DateTime.UtcNow.AddHours(-2))
                .Include(s => s.Party)
                .OrderByDescending(s => s.SeatedAt)
                .Take(20)
                .ToListAsync();

            // 2. Average actual wait time
            double avgWait = recentSeatings
                .Where(s => s.Party.ActualWaitMinutes.HasValue)
                .Select(s => (double)s.Party.ActualWaitMinutes.Value)
                .DefaultIfEmpty(10)
                .Average();

            // 3. Available tables
            int availableTables = await _context.RestaurantTables
                .CountAsync(t => t.Status == "Available" && t.CurrentPartyId == null);

            double tableWeight = availableTables == 0 ? 1.5 : 1.0;

            // 4. Party size weight
            double sizeWeight = 1.0 + (partySize - 2) * 0.10;
            if (sizeWeight < 1.0)
                sizeWeight = 1.0;

            // 5. Core formula
            double estimate = (position + 1) * avgWait * sizeWeight * tableWeight;

            // 6. Bounds
            if (estimate < 5)
                estimate = 5;

            if (estimate > 90)
                estimate = 90;

            return (int)Math.Round(estimate);
        }
    }
}
