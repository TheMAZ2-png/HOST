using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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
            {
                return Page();
            }

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
                OwnerId = user.Id
            };

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
