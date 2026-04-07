using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages.RestaurantTables
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<RestaurantTable> RestaurantTables { get; set; } = new List<RestaurantTable>();

        public async Task OnGetAsync()
        {
            RestaurantTables = await _context.RestaurantTables
                .Include(t => t.CurrentParty)
                .AsNoTracking()
                .OrderBy(t => t.TableNumber)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostClearAllAsync()
        {
            if (!User.IsInRole("Manager") && !User.IsInRole("Host"))
                return Unauthorized();

            var tables = await _context.RestaurantTables
                .Include(t => t.Seatings)
                .Include(t => t.CurrentParty)
                .ToListAsync();

            foreach (var table in tables)
            {
                if (table.CurrentParty != null)
                {
                    var party = table.CurrentParty;

                    var activeSeating = table.Seatings
                        .Where(s => s.ClearedAt == null)
                        .OrderByDescending(s => s.SeatedAt)
                        .FirstOrDefault();

                    if (activeSeating != null)
                        activeSeating.ClearedAt = DateTime.UtcNow;

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
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "All tables cleared and all parties marked as completed.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAllAsync()
        {
            if (!User.IsInRole("Manager"))
                return Unauthorized();

            var seatings = await _context.Seatings.ToListAsync();
            _context.Seatings.RemoveRange(seatings);

            var tables = await _context.RestaurantTables.ToListAsync();
            foreach (var table in tables)
            {
                table.CurrentPartyId = null;
                table.Status = "Available";
            }

            await _context.SaveChangesAsync();

            _context.RestaurantTables.RemoveRange(tables);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "All restaurant tables have been permanently deleted.";
            return RedirectToPage();
        }
    }
}
