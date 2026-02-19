using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HOST.Pages
{
    [AllowAnonymous]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
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

            var party = new Party
            {
                PartyName = PartyRegistration.PartyName,
                PhoneNumber = PartyRegistration.PhoneNumber,
                PartySize = PartyRegistration.PartySize,
                Notes = PartyRegistration.Notes
            };

            _context.Parties.Add(party);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Parties/Registered", new { id = party.PartyId });
        }

        public class PartyRegistrationInput
        {
            [System.ComponentModel.DataAnnotations.Required]
            [System.ComponentModel.DataAnnotations.StringLength(80)]
            public string PartyName { get; set; }

            [System.ComponentModel.DataAnnotations.StringLength(20)]
            public string? PhoneNumber { get; set; }

            [System.ComponentModel.DataAnnotations.Required]
            [System.ComponentModel.DataAnnotations.Range(1, 12)]
            public int PartySize { get; set; }

            [System.ComponentModel.DataAnnotations.StringLength(250)]
            public string? Notes { get; set; }
        }
    }
}
