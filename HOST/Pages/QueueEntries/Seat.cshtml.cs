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

        // ---------------------------------------------------------
        // GET
        // ---------------------------------------------------------
        public async Task<IActionResult> OnGetAsync(int id)
        {
            QueueEntry = await _context.QueueEntries
                .Include(q => q.Party)
                .FirstOrDefaultAsync(q => q.QueueEntryId == id);

            if (QueueEntry == null)
                return NotFound();

            Party = QueueEntry.Party;

            AvailableTables = await _context.RestaurantTables
                .Where(t => t.Status == "Available" && t.CurrentPartyId == null)
                .OrderBy(t => t.TableNumber)
                .ToListAsync();

            Servers = await _context.Employees
                .Where(e => e.Role == "Server")
                .OrderBy(e => e.DisplayName ?? e.Name)
                .ToListAsync();

            return Page();
        }

        // ---------------------------------------------------------
        // POST (with full debugging)
        // ---------------------------------------------------------
        public async Task<IActionResult> OnPostAsync(int id)
        {
            TempData["Debug"] = $"POST HIT → id={id}";
            Console.WriteLine($"DEBUG: POST HIT → id={id}");

            var queueEntry = await _context.QueueEntries
                .Include(q => q.Party)
                .FirstOrDefaultAsync(q => q.QueueEntryId == id);

            if (queueEntry == null)
            {
                TempData["ErrorMessage"] = "Queue entry not found.";
                TempData["Debug"] = "FAIL → queueEntry null";
                return RedirectToPage("./Index");
            }

            if (queueEntry.Party == null)
            {
                TempData["ErrorMessage"] = "This queue entry has no associated party.";
                TempData["Debug"] = "FAIL → Party null";
                return RedirectToPage("./Index");
            }

            if (queueEntry.Status == "Seated")
            {
                TempData["ErrorMessage"] = "This party is already seated.";
                TempData["Debug"] = "FAIL → Already seated";
                return RedirectToPage("./Index");
            }

            TempData["Debug"] = $"PASS → queueEntry OK, SelectedTableId={SelectedTableId}, SelectedServerId={SelectedServerId}";
            Console.WriteLine(TempData["Debug"]);

            var table = await _context.RestaurantTables
                .Include(t => t.Seatings)
                .FirstOrDefaultAsync(t => t.TableId == SelectedTableId);

            if (table == null)
            {
                TempData["ErrorMessage"] = "Selected table not found.";
                TempData["Debug"] = "FAIL → table null";
                return RedirectToPage("./Index");
            }

            if (table.Status != "Available" || table.CurrentPartyId != null)
            {
                TempData["ErrorMessage"] = "Selected table is not available.";
                TempData["Debug"] = $"FAIL → table not available (Status={table.Status}, CurrentPartyId={table.CurrentPartyId})";
                return RedirectToPage("./Index");
            }

            var server = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == SelectedServerId && e.Role == "Server");

            if (server == null)
            {
                TempData["ErrorMessage"] = "Selected server not found.";
                TempData["Debug"] = "FAIL → server null";
                return RedirectToPage("./Index");
            }

            TempData["Debug"] = "PASS → table + server OK";
            Console.WriteLine(TempData["Debug"]);

            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var seatingEmployee = await _context.Employees
                .FirstOrDefaultAsync(e => e.IdentityUserId == identityUserId);

            if (seatingEmployee == null)
            {
                TempData["ErrorMessage"] = "Unable to determine logged-in employee.";
                TempData["Debug"] = "FAIL → seatingEmployee null";
                return RedirectToPage("./Index");
            }

            TempData["Debug"] = "PASS → seatingEmployee OK";
            Console.WriteLine(TempData["Debug"]);

            // Update queue entry
            queueEntry.Status = "Completed";

            // Create seating record
            var seating = new Seating
            {
                PartyId = queueEntry.PartyId,
                RestaurantTableId = table.TableId,
                AssignedServerId = SelectedServerId,
                SeatedByEmployeeId = seatingEmployee.EmployeeId,
                SeatedAt = DateTime.UtcNow
            };

            _context.Seatings.Add(seating);

            // Update table
            table.Status = "Occupied";
            table.CurrentPartyId = queueEntry.PartyId;

            // Update party
            queueEntry.Party.Status = "Seated";

            TempData["Debug"] = "PASS → Saving changes…";
            Console.WriteLine(TempData["Debug"]);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Party successfully seated.";
            TempData["Debug"] = "PASS → Save complete";
            Console.WriteLine(TempData["Debug"]);

            return RedirectToPage("./Index");
        }
    }
}
