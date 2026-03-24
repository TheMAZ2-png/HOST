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
            Party = await _context.Parties.FirstOrDefaultAsync(p => p.PartyId == id);

            if (Party == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Allow Managers OR the owner
            if (User.IsInRole("Manager") || Party.OwnerId == userId)
                return Page();

            return Forbid();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var partyInDb = await _context.Parties.FirstOrDefaultAsync(p => p.PartyId == Party.PartyId);

            if (partyInDb == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Only Managers or the owner can update
            if (!User.IsInRole("Manager") && partyInDb.OwnerId != userId)
                return Forbid();

            // ⭐ Ensure EF Core is tracking the entity
            _context.Attach(partyInDb);

            // Update allowed fields
            partyInDb.PartyName = Party.PartyName;
            partyInDb.PhoneNumber = Party.PhoneNumber;
            partyInDb.PartySize = Party.PartySize;
            partyInDb.Notes = Party.Notes;

            // ⭐ Sync QueueEntry (only if still waiting)
            var queueEntry = await _context.QueueEntries
                .FirstOrDefaultAsync(q => q.PartyId == partyInDb.PartyId && q.Status == "Waiting");

            if (queueEntry != null)
            {
                queueEntry.UpdatedAt = DateTime.UtcNow;

                // Optional: recalc wait time
                // queueEntry.EstimatedWaitMinutes = CalculateWaitTime(partyInDb.PartySize);
            }

            await _context.SaveChangesAsync();

            return RedirectToPage("Index");
        }

        // ⭐ Delete handler with Guest sign-out
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var party = await _context.Parties.FirstOrDefaultAsync(p => p.PartyId == id);

            if (party == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            bool isOwner = party.OwnerId == userId;
            bool isStaff = User.IsInRole("Manager") || User.IsInRole("Server") || User.IsInRole("Host");

            // Only Managers or the owner can delete
            if (!isOwner && !isStaff)
                return Forbid();

            _context.Parties.Remove(party);
            await _context.SaveChangesAsync();

            // ⭐ If a Guest deletes their own party → sign out + redirect home
            if (isOwner && !isStaff)
            {
                await _signInManager.SignOutAsync();
                return RedirectToPage("/Index");
            }

            // ⭐ Staff deleting → go back to Parties list
            return RedirectToPage("Index");
        }
    }
}
