using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace HOST.Pages.Parties
{
    [Authorize(Roles = "Manager")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public PartyInputModel PartyInput { get; set; } = new();

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            // Create Party
            var party = new Party
            {
                PartyName = PartyInput.PartyName,
                PhoneNumber = PartyInput.PhoneNumber,
                PartySize = PartyInput.PartySize,
                Notes = PartyInput.Notes,
                OwnerId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                Status = "Waiting",
                CreatedAt = DateTime.UtcNow
            };

            // ⭐ NEW: Store estimated wait time at join using improved algorithm
            party.EstimatedWaitAtJoin = await CalculateEstimatedWaitAtJoinAsync(party.PartySize);

            _context.Parties.Add(party);
            await _context.SaveChangesAsync();

            // Create QueueEntry
            var queueEntry = new QueueEntry
            {
                PartyId = party.PartyId,
                Status = "Waiting",
                CreatedAt = DateTime.UtcNow
            };

            _context.QueueEntries.Add(queueEntry);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Party created and added to the queue.";
            return RedirectToPage("./Index");
        }

        // ============================================================
        // ⭐ NEW: Calculate estimated wait time at join
        // ============================================================
        private async Task<int> CalculateEstimatedWaitAtJoinAsync(int partySize)
        {
            // Determine the new party's position (last in line)
            var waiting = await _context.Parties
                .Where(p => p.Status == "Waiting")
                .OrderBy(p => p.CreatedAt)
                .ToListAsync();

            int position = waiting.Count;

            return await CalculateEstimatedWaitAsync(partySize, position);
        }

        // ============================================================
        // ⭐ NEW: Full wait-time algorithm (self-contained)
        // ============================================================
        private async Task<int> CalculateEstimatedWaitAsync(int partySize, int position)
        {
            // 1. Get recent seatings (last 2 hours)
            var recentSeatings = await _context.Seatings
                .Where(s => s.SeatedAt > DateTime.UtcNow.AddHours(-2))
                .Include(s => s.Party)
                .OrderByDescending(s => s.SeatedAt)
                .Take(20)
                .ToListAsync();

            // 2. Compute average actual wait time
            double avgWait = recentSeatings
                .Where(s => s.Party.ActualWaitMinutes.HasValue)
                .Select(s => (double)s.Party.ActualWaitMinutes.Value)
                .DefaultIfEmpty(10) // fallback if no data
                .Average();

            // 3. Count available tables (safe logic)
            int availableTables = await _context.RestaurantTables
                .CountAsync(t => t.Status == "Available" && t.CurrentPartyId == null);

            double tableWeight = availableTables == 0 ? 1.5 : 1.0;

            // 4. Party size weight
            double sizeWeight = 1.0 + (partySize - 2) * 0.10;
            if (sizeWeight < 1.0)
                sizeWeight = 1.0;

            // 5. Core formula
            double estimate = (position + 1) * avgWait * sizeWeight * tableWeight;

            // 6. Minimum and maximum bounds
            if (estimate < 5)
                estimate = 5;

            if (estimate > 90)
                estimate = 90;

            return (int)Math.Round(estimate);
        }

        // ============================================================
        // Input Model
        // ============================================================
        public class PartyInputModel
        {
            [Required]
            [StringLength(80)]
            public string PartyName { get; set; }

            [Required]
            [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone number must be exactly 10 digits.")]
            public string PhoneNumber { get; set; }

            [Required]
            [Range(1, 12)]
            public int PartySize { get; set; }

            [StringLength(250)]
            public string? Notes { get; set; }
        }
    }
}
