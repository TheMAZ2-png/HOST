using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HOST.Pages
{
    [AllowAnonymous]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public IndexModel(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [BindProperty]
        public PartyRegistrationInput PartyRegistration { get; set; } = new();

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            // 1. Prevent duplicate phone numbers
            var existingUser = await _userManager.FindByNameAsync(PartyRegistration.PhoneNumber);
            if (existingUser != null)
            {
                ModelState.AddModelError("PartyRegistration.PhoneNumber", "This phone number is already registered.");
                return Page();
            }

            // 2. Create the Identity user
            var user = new IdentityUser
            {
                UserName = PartyRegistration.PhoneNumber,
                Email = $"{Guid.NewGuid()}@guest.local"
            };

            var password = Guid.NewGuid().ToString("N") + "!aA1";
            var result = await _userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Unable to create user.");
                return Page();
            }

            await _userManager.AddToRoleAsync(user, "Guest");
            await _userManager.AddClaimAsync(user, new Claim("PartyName", PartyRegistration.PartyName));

            // 3. Create the Party
            var party = new Party
            {
                PartyName = PartyRegistration.PartyName,
                PhoneNumber = PartyRegistration.PhoneNumber,
                PartySize = PartyRegistration.PartySize,
                Notes = PartyRegistration.Notes,
                OwnerId = user.Id,
                Status = "Waiting",
                CreatedAt = DateTime.UtcNow
            };

            // ⭐ NEW: Store estimated wait time at join using improved algorithm
            party.EstimatedWaitAtJoin = await CalculateEstimatedWaitAtJoinAsync(party.PartySize);

            _context.Parties.Add(party);
            await _context.SaveChangesAsync();

            // 4. Automatically create a QueueEntry
            var queueEntry = new QueueEntry
            {
                PartyId = party.PartyId,
                Status = "Waiting",
                CreatedAt = DateTime.UtcNow
            };

            _context.QueueEntries.Add(queueEntry);
            await _context.SaveChangesAsync();

            // 5. Sign in the guest
            await _signInManager.SignInAsync(user, isPersistent: false);

            // 6. Redirect to guest party dashboard
            return RedirectToPage("/Parties/Index");
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
        public class PartyRegistrationInput
        {
            [System.ComponentModel.DataAnnotations.Required]
            [System.ComponentModel.DataAnnotations.StringLength(80)]
            public string PartyName { get; set; }

            [System.ComponentModel.DataAnnotations.Required]
            [System.ComponentModel.DataAnnotations.RegularExpression(
                @"^\d{10}$",
                ErrorMessage = "Phone number must be exactly 10 digits.")]
            public string PhoneNumber { get; set; }

            [System.ComponentModel.DataAnnotations.Required]
            [System.ComponentModel.DataAnnotations.Range(1, 12)]
            public int PartySize { get; set; }

            [System.ComponentModel.DataAnnotations.StringLength(250)]
            public string? Notes { get; set; }
        }
    }
}
