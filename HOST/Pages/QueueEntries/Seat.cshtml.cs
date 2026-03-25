using System.Security.Claims;
using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

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
        public List<RestaurantTable> AvailableTables { get; set; }
        public List<Employee> Servers { get; set; }

        [BindProperty]
        public int SelectedTableId { get; set; }

        [BindProperty]
        public int SelectedServerId { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            QueueEntry = await _context.QueueEntries
                .Include(q => q.Party)
                .FirstOrDefaultAsync(q => q.QueueEntryId == id);

            if (QueueEntry == null)
            {
                TempData["ErrorMessage"] = $"Queue entry {id} was not found.";
                return RedirectToPage("./Index");
            }

            if (QueueEntry.Party == null)
            {
                TempData["ErrorMessage"] = "This queue entry has no associated party.";
                return RedirectToPage("./Index");
            }

            if (QueueEntry.Status == "Seated")
            {
                TempData["ErrorMessage"] = "This party is already seated.";
                return RedirectToPage("./Index");
            }

            var existingTable = await _context.RestaurantTables
                .FirstOrDefaultAsync(t => t.CurrentPartyId == QueueEntry.PartyId);

            if (existingTable != null)
            {
                TempData["ErrorMessage"] =
                    $"This party is already seated at Table {existingTable.TableNumber}.";
                return RedirectToPage("./Index");
            }

            Party = QueueEntry.Party;

            AvailableTables = await _context.RestaurantTables
                .Where(t => t.Status == "Available" && t.IsActive)
                .OrderBy(t => t.TableNumber)
                .ToListAsync();

            Servers = await _context.Employees
                .Where(e => e.Role == "Server")
                .OrderBy(e => e.DisplayName ?? e.Name)
                .ToListAsync();

            return Page();
        }

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

            // ⭐ Create seating record
            var seating = new Seating
            {
                PartyId = queueEntry.PartyId,
                RestaurantTableId = table.TableId,
                AssignedServerId = SelectedServerId,
                SeatedByEmployeeId = employee.EmployeeId,
                SeatedAt = DateTime.UtcNow
            };

            _context.Seatings.Add(seating);

            // ⭐ Update table
            table.Status = "Occupied";
            table.CurrentPartyId = queueEntry.PartyId;

            // ⭐ Update queue entry
            queueEntry.Status = "Seated";
            queueEntry.SeatedAt = DateTime.UtcNow;

            // ⭐ Save seating + table updates BEFORE deleting Party
            await _context.SaveChangesAsync();

            // ⭐ Delete QueueEntry + Party
            _context.QueueEntries.Remove(queueEntry);
            _context.Parties.Remove(queueEntry.Party);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Party successfully seated.";
            return RedirectToPage("./Index");
        }
    }
}
