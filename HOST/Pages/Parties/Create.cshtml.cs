using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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
                Status = "Waiting"
            };

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

            return RedirectToPage("./Index");
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
