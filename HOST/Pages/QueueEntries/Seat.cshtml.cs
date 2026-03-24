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
                return NotFound($"QueueEntry not found for id={id}");

            if (QueueEntry.Party == null)
                return NotFound($"Party not found for QueueEntry id={id}");

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

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var queueEntry = await _context.QueueEntries
                .Include(q => q.Party)
                .FirstOrDefaultAsync(q => q.QueueEntryId == id);

            if (queueEntry == null)
                return NotFound($"QueueEntry not found for id={id}");

            if (queueEntry.Party == null)
                return NotFound($"Party not found for QueueEntry id={id}");

            var table = await _context.RestaurantTables
                .FirstOrDefaultAsync(t => t.TableId == SelectedTableId);

            if (table == null)
                return NotFound($"Table not found for id={SelectedTableId}");

            // Logged-in IdentityUser
            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.IdentityUserId == identityUserId);

            if (employee == null)
            {
                ModelState.AddModelError("", "Unable to determine the logged-in employee.");
                return Page();
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

            // Update queue entry
            queueEntry.Status = "Seated";
            queueEntry.SeatedAt = DateTime.UtcNow;

            // Update party
            queueEntry.Party.Status = "Seated";

            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
