using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;   // <-- REQUIRED for adding claims

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

            // 1. Create the Party
            var party = new Party
            {
                PartyName = PartyRegistration.PartyName,
                PhoneNumber = PartyRegistration.PhoneNumber,
                PartySize = PartyRegistration.PartySize,
                Notes = PartyRegistration.Notes
            };

            _context.Parties.Add(party);
            await _context.SaveChangesAsync();

            // 2. Create an Identity user for this guest
            var user = new IdentityUser
            {
                UserName = PartyRegistration.PhoneNumber
                           ?? PartyRegistration.PartyName.Replace(" ", "") + party.PartyId,
                Email = $"{Guid.NewGuid()}@guest.local"
            };

            var password = Guid.NewGuid().ToString("N") + "!aA1";

            var result = await _userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                return Page();
            }

            await _userManager.AddClaimAsync(user, new Claim("PartyName", party.PartyName));

            // 3. Sign the user in automatically
            await _signInManager.SignInAsync(user, isPersistent: false);

            // 4. Redirect to queue page
            return RedirectToPage("/Parties/Index", new { id = party.PartyId });
        }

        public class PartyRegistrationInput
        {
            [System.ComponentModel.DataAnnotations.Required]
            [System.ComponentModel.DataAnnotations.StringLength(80)]
            public string PartyName { get; set; }

            // ⭐ UPDATED: digits‑only, exactly 10 digits ⭐
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
