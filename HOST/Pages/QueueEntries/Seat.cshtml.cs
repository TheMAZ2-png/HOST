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
                return NotFound();

            Party = QueueEntry.Party;

            // Load available tables
            AvailableTables = await _context.RestaurantTables
                .Where(t => t.Status == "Available" && t.IsActive)
                .OrderBy(t => t.TableNumber)
                .ToListAsync();

            // Load servers
            Servers = await _context.Employees
                .Where(e => e.Role == "Server")
                .OrderBy(e => e.LastName)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var queueEntry = await _context.QueueEntries
                .Include(q => q.Party)
                .FirstOrDefaultAsync(q => q.QueueEntryId == id);

            if (queueEntry == null)
                return NotFound();

            var table = await _context.RestaurantTables
                .FirstOrDefaultAsync(t => t.TableId == SelectedTableId);

            if (table == null)
                return NotFound();

            // Create Seating record
            var seating = new Seating
            {
                PartyId = queueEntry.PartyId,
                RestaurantTableId = table.TableId,
                AssignedServerId = SelectedServerId,
                SeatedByEmployeeId = 0, // TODO: Replace with logged-in employee ID
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
