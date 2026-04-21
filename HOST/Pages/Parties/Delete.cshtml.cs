using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HOST.Pages.Parties
{
    [Authorize] // ⭐ Allow all authenticated users; we enforce permissions manually
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Party Party { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            Party = await _context.Parties
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PartyId == id);

            if (Party == null)
                return NotFound();

            // ⭐ Permission check for GET
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool isManager = User.IsInRole("Manager");
            bool isHost = User.IsInRole("Host");
            bool isServer = User.IsInRole("Server");
            bool isStaff = isManager || isHost || isServer;
            bool isOwner = Party.OwnerId == userId;

            if (!isStaff && !isOwner)
                return Forbid();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var party = await _context.Parties
                .FirstOrDefaultAsync(p => p.PartyId == Party.PartyId);

            if (party == null)
                return NotFound();

            // ⭐ Permission check for POST
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool isManager = User.IsInRole("Manager");
            bool isHost = User.IsInRole("Host");
            bool isServer = User.IsInRole("Server");
            bool isStaff = isManager || isHost || isServer;
            bool isOwner = party.OwnerId == userId;

            if (!isStaff && !isOwner)
                return Forbid();

            // Already deleted?
            if (party.IsDeleted)
                return RedirectToPage("./Index");

            // Only Waiting parties can be deleted
            if (party.Status != "Waiting")
            {
                TempData["ErrorMessage"] = "Only parties that are still waiting can be deleted.";
                return RedirectToPage("./Index");
            }

            // ⭐ Soft delete
            party.IsDeleted = true;
            party.DeletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Party deleted successfully.";
            return RedirectToPage("./Index");
        }
    }
}
