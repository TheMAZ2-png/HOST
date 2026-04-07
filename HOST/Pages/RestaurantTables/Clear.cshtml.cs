using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages.RestaurantTables
{
    [Authorize]
    public class ClearModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ClearModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public RestaurantTable Table { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Table = await _context.RestaurantTables
                .Include(t => t.CurrentParty)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TableId == id);

            if (Table == null)
                return NotFound();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var table = await _context.RestaurantTables
                .Include(t => t.Seatings)
                .Include(t => t.CurrentParty)
                .FirstOrDefaultAsync(t => t.TableId == id);

            if (table == null)
                return NotFound();

            if (table.CurrentPartyId == null)
            {
                TempData["ErrorMessage"] = "This table is already clear.";
                return RedirectToPage("Index");
            }

            var activeSeating = table.Seatings
                .Where(s => s.ClearedAt == null)
                .OrderByDescending(s => s.SeatedAt)
                .FirstOrDefault();

            if (activeSeating != null)
                activeSeating.ClearedAt = DateTime.UtcNow;

            var party = table.CurrentParty;
            if (party != null)
            {
                party.Status = "Completed";
                party.CompletedAt = DateTime.UtcNow;

                party.ActualWaitMinutes =
                    (int)Math.Floor((DateTime.UtcNow - party.CreatedAt).TotalMinutes);

                if (!party.EstimatedWaitAtJoin.HasValue)
                    party.EstimatedWaitAtJoin = party.EstimatedWaitMinutes;

                var queueEntries = await _context.QueueEntries
                    .Where(q => q.PartyId == party.PartyId)
                    .ToListAsync();

                _context.QueueEntries.RemoveRange(queueEntries);
            }

            table.Status = "Available";
            table.CurrentPartyId = null;
            table.CurrentParty = null;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Table cleared successfully.";
            return RedirectToPage("Index");
        }
    }
}
