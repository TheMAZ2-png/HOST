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

            var today = DateTime.UtcNow.Date;

            // ⭐ NEW RULE: Only block if same phone number already joined TODAY
            var existingTodayParty = await _context.Parties
                .Where(p =>
                    p.PhoneNumber == PartyRegistration.PhoneNumber &&
                    !p.IsDeleted &&
                    p.CreatedAt.Date == today)
                .FirstOrDefaultAsync();

            if (existingTodayParty != null)
            {
                ModelState.AddModelError("PartyRegistration.PhoneNumber",
                    "This phone number already has a party on the waitlist today.");
                return Page();
            }

            // ⭐ Identity user check stays the same (one account per phone number)
            var existingUser = await _userManager.FindByNameAsync(PartyRegistration.PhoneNumber);
            IdentityUser user;

            if (existingUser == null)
            {
                // Create new guest user
                user = new IdentityUser
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
            }
            else
            {
                // Reuse existing guest account
                user = existingUser;
            }

            // ⭐ Create Party (soft-delete fields included)
            var party = new Party
            {
                PartyName = PartyRegistration.PartyName,
                PhoneNumber = PartyRegistration.PhoneNumber,
                PartySize = PartyRegistration.PartySize,
                Notes = PartyRegistration.Notes,
                OwnerId = user.Id,
                Status = "Waiting",
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false,
                DeletedAt = null
            };

            // ⭐ Store estimated wait time at join
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

            // Sign in guest
            await _signInManager.SignInAsync(user, isPersistent: false);

            return RedirectToPage("/Parties/Index");
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
