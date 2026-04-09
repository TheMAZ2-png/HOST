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

            var today = DateTime.UtcNow.Date;

            // ⭐ NEW RULE: Only block if same phone number already used TODAY
            var existingTodayParty = await _context.Parties
                .Where(p =>
                    p.PhoneNumber == PartyInput.PhoneNumber &&
                    !p.IsDeleted &&
                    p.CreatedAt.Date == today)
                .FirstOrDefaultAsync();

            if (existingTodayParty != null)
            {
                ModelState.AddModelError("PartyInput.PhoneNumber",
                    "A party with this phone number already exists today.");
                return Page();
            }

            // Create Party
            var party = new Party
            {
                PartyName = PartyInput.PartyName,
                PhoneNumber = PartyInput.PhoneNumber,
                PartySize = PartyInput.PartySize,
                Notes = PartyInput.Notes,
                OwnerId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                Status = "Waiting",
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false,
                DeletedAt = null
            };

            // Store estimated wait time at join
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

        private async Task<int> CalculateEstimatedWaitAtJoinAsync(int partySize)
        {
            var waiting = await _context.Parties
                .Where(p => p.Status == "Waiting" && !p.IsDeleted)
                .OrderBy(p => p.CreatedAt)
                .ToListAsync();

            int position = waiting.Count;

            return await CalculateEstimatedWaitAsync(partySize, position);
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
