using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HOST.Data;
using HOST.Models;

namespace HOST.Pages.Parties
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly SignInManager<IdentityUser> _signInManager;

        public EditModel(ApplicationDbContext context, SignInManager<IdentityUser> signInManager)
        {
            _context = context;
            _signInManager = signInManager;
        }

        [BindProperty]
        public Party Party { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Party = await _context.Parties
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PartyId == id);

            if (Party == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Managers can edit anything
            if (User.IsInRole("Manager"))
                return Page();

            // Guests can edit only their own Waiting party
            if (Party.OwnerId == userId && Party.Status == "Waiting")
                return Page();

            return Forbid();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var partyInDb = await _context.Parties
                .FirstOrDefaultAsync(p => p.PartyId == Party.PartyId);

            if (partyInDb == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            bool isManager = User.IsInRole("Manager");
            bool isOwner = partyInDb.OwnerId == userId;

            // Guests can only edit Waiting parties
            if (!isManager && (!isOwner || partyInDb.Status != "Waiting"))
                return Forbid();

            // Update allowed fields
            partyInDb.PartyName = Party.PartyName;
            partyInDb.PhoneNumber = Party.PhoneNumber;
            partyInDb.PartySize = Party.PartySize;
            partyInDb.Notes = Party.Notes;

            // Update queue entry timestamp if still waiting
            var queueEntry = await _context.QueueEntries
                .FirstOrDefaultAsync(q => q.PartyId == partyInDb.PartyId && q.Status == "Waiting");

            if (queueEntry != null)
                queueEntry.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return RedirectToPage("Index");
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var party = await _context.Parties.FirstOrDefaultAsync(p => p.PartyId == id);

            if (party == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            bool isOwner = party.OwnerId == userId;
            bool isManager = User.IsInRole("Manager");

            // Guests can only delete Waiting parties
            if (!isManager && (!isOwner || party.Status != "Waiting"))
                return Forbid();

            // Delete queue entries
            var queueEntries = _context.QueueEntries.Where(q => q.PartyId == party.PartyId);
            _context.QueueEntries.RemoveRange(queueEntries);

            _context.Parties.Remove(party);
            await _context.SaveChangesAsync();

            // Guests deleting their own party → sign out
            if (isOwner && !isManager)
            {
                await _signInManager.SignOutAsync();
                return RedirectToPage("/Index");
            }

            return RedirectToPage("Index");
        }
    }
}
