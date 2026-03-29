using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HOST.Pages.QueueEntries
{
    [Authorize(Roles = "Manager,Host,Server")]
    public class SeatModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public SeatModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public QueueEntry QueueEntry { get; set; }
        public Party Party { get; set; }
        public List<RestaurantTable> AvailableTables { get; set; } = new();
        public List<Employee> Servers { get; set; } = new();

        [BindProperty]
        public int SelectedTableId { get; set; }

        [BindProperty]
        public int SelectedServerId { get; set; }

        // -----------------------------
        // GET
        // -----------------------------
        public async Task<IActionResult> OnGetAsync(int id)
        {
            QueueEntry = await _context.QueueEntries
                .Include(q => q.Party)
                .FirstOrDefaultAsync(q => q.QueueEntryId == id);

            if (QueueEntry == null)
                return NotFound();

            Party = QueueEntry.Party;

            AvailableTables = await _context.RestaurantTables
                .Where(t => t.Status == "Available")
                .OrderBy(t => t.TableNumber)
                .ToListAsync();

            Servers = await _context.Employees
                .Where(e => e.Role == "Server")
                .OrderBy(e => e.DisplayName ?? e.Name)
                .ToListAsync();

            return Page();
        }

        // -----------------------------
        // POST — Seat the party
        // -----------------------------
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAsync(int id)
        {
            var queueEntry = await _context.QueueEntries
                .Include(q => q.Party)
                .FirstOrDefaultAsync(q => q.QueueEntryId == id);

            if (queueEntry == null)
            {
                TempData["ErrorMessage"] = "Queue entry not found.";
                return RedirectToPage("./Index");
            }

            if (queueEntry.Party == null)
            {
                TempData["ErrorMessage"] = "This queue entry has no associated party.";
                return RedirectToPage("./Index");
            }

            if (queueEntry.Status == "Seated")
            {
                TempData["ErrorMessage"] = "This party is already seated.";
                return RedirectToPage("./Index");
            }

            var table = await _context.RestaurantTables
                .FirstOrDefaultAsync(t => t.TableId == SelectedTableId);

            if (table == null)
            {
                TempData["ErrorMessage"] = "Selected table not found.";
                return RedirectToPage("./Index");
            }

            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.IdentityUserId == identityUserId);

            if (employee == null)
            {
                TempData["ErrorMessage"] = "Unable to determine logged-in employee.";
                return RedirectToPage("./Index");
            }

            // ⭐ NEW: Calculate actual wait time
            var activeQueueEntry = await _context.QueueEntries
                .Where(q => q.PartyId == queueEntry.PartyId && q.Status == "Waiting")
                .OrderByDescending(q => q.CreatedAt)
                .FirstOrDefaultAsync();

            if (activeQueueEntry != null)
            {
                queueEntry.Party.ActualWaitMinutes =
                    (int)(DateTime.UtcNow - activeQueueEntry.CreatedAt).TotalMinutes;
            }

            // Create seating record
            var seating = new Seating
            {
                PartyId = queueEntry.PartyId,
                RestaurantTableId = table.TableId,
                AssignedServerId = SelectedServerId,
                SeatedByEmployeeId = employee.EmployeeId,
                SeatedAt = DateTime.UtcNow
            };

            _context.Seatings.Add(seating);

            // Update table
            table.Status = "Occupied";
            table.CurrentPartyId = queueEntry.PartyId;

            // Update party
            queueEntry.Party.Status = "Seated";

            // Update queue entry
            queueEntry.Status = "Seated";
            queueEntry.SeatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Remove queue entry
            _context.QueueEntries.Remove(queueEntry);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Party successfully seated.";
            return RedirectToPage("./Index");
        }
    }
}
