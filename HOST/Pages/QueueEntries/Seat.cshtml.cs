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

        // ⭐ FIX: ValidateAntiForgeryToken belongs HERE, not on the class
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAsync(int id)
        {
            Console.WriteLine("POST HIT: Starting OnPostAsync");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("FAIL: ModelState invalid");
                TempData["ErrorMessage"] = "Form submission failed validation.";
                return RedirectToPage("./Index");
            }

            var queueEntry = await _context.QueueEntries
                .Include(q => q.Party)
                .FirstOrDefaultAsync(q => q.QueueEntryId == id);

            if (queueEntry == null)
            {
                Console.WriteLine("FAIL: queueEntry null");
                TempData["ErrorMessage"] = "Queue entry not found.";
                return RedirectToPage("./Index");
            }

            if (queueEntry.Party == null)
            {
                Console.WriteLine("FAIL: Party null");
                TempData["ErrorMessage"] = "This queue entry has no associated party.";
                return RedirectToPage("./Index");
            }

            if (queueEntry.Status == "Seated")
            {
                Console.WriteLine("FAIL: Already seated");
                TempData["ErrorMessage"] = "This party is already seated.";
                return RedirectToPage("./Index");
            }

            var table = await _context.RestaurantTables
                .FirstOrDefaultAsync(t => t.TableId == SelectedTableId);

            if (table == null)
            {
                Console.WriteLine($"FAIL: Table {SelectedTableId} not found");
                TempData["ErrorMessage"] = "Selected table not found.";
                return RedirectToPage("./Index");
            }

            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Console.WriteLine($"identityUserId = {identityUserId}");

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.IdentityUserId == identityUserId);

            if (employee == null)
            {
                Console.WriteLine("FAIL: employee null");
                TempData["ErrorMessage"] = "Unable to determine logged-in employee.";
                return RedirectToPage("./Index");
            }

            Console.WriteLine("SUCCESS: All checks passed, seating party now.");

            var seating = new Seating
            {
                PartyId = queueEntry.PartyId,
                RestaurantTableId = table.TableId,
                AssignedServerId = SelectedServerId,
                SeatedByEmployeeId = employee.EmployeeId,
                SeatedAt = DateTime.UtcNow
            };

            _context.Seatings.Add(seating);

            table.Status = "Occupied";
            table.CurrentPartyId = queueEntry.PartyId;
            _context.RestaurantTables.Update(table);

            queueEntry.Status = "Seated";
            queueEntry.SeatedAt = DateTime.UtcNow;
            queueEntry.Party.Status = "Seated";

            await _context.SaveChangesAsync();

            Console.WriteLine("SUCCESS: SaveChanges completed.");

            TempData["SuccessMessage"] = "Party successfully seated.";
            return RedirectToPage("./Index");
        }
    }
}
