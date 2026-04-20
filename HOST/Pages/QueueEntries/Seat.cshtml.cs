using HOST.Data;
using HOST.Models;
using HOST.Services;
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
        private readonly TwilioVoiceCallService _twilioService;
        private readonly ILogger<SeatModel> _logger;

        public SeatModel(
            ApplicationDbContext context,
            TwilioVoiceCallService twilioService,
            ILogger<SeatModel> logger)
        {
            _context = context;
            _twilioService = twilioService;
            _logger = logger;
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

            // ⭐ Only show tables with enough seats
            AvailableTables = await _context.RestaurantTables
                .Where(t =>
                    t.Status == "Available" &&
                    t.CurrentPartyId == null &&
                    t.SeatCapacity >= Party.PartySize)
                .OrderBy(t => t.TableNumber)
                .ToListAsync();

            Servers = await _context.Employees
                .Where(e => e.Role == "Server")
                .OrderBy(e => e.DisplayName ?? e.Name)
                .ToListAsync();

            return Page();
        }

        // ---------------------------------------------------------
        // POST
        // ---------------------------------------------------------
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
                .Include(t => t.Seatings)
                .FirstOrDefaultAsync(t => t.TableId == SelectedTableId);

            if (table == null || table.Status != "Available" || table.CurrentPartyId != null)
            {
                TempData["ErrorMessage"] = "Selected table is not available.";
                return RedirectToPage("./Index");
            }

            // ⭐ Double-check seat capacity on POST
            if (table.SeatCapacity < queueEntry.Party.PartySize)
            {
                TempData["ErrorMessage"] = "Selected table does not have enough seats.";
                return RedirectToPage("./Index");
            }

            var server = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == SelectedServerId && e.Role == "Server");

            if (server == null)
            {
                TempData["ErrorMessage"] = "Selected server not found.";
                return RedirectToPage("./Index");
            }

            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var seatingEmployee = await _context.Employees
                .FirstOrDefaultAsync(e => e.IdentityUserId == identityUserId);

            if (seatingEmployee == null)
            {
                TempData["ErrorMessage"] = "Unable to determine logged-in employee.";
                return RedirectToPage("./Index");
            }

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

            await _context.SaveChangesAsync();

            // ⭐ Place Twilio voice call to notify party their table is ready
            _logger.LogInformation("🔔 Attempting to call party {PartyName} at {PhoneNumber}", 
                queueEntry.Party.PartyName, 
                queueEntry.Party.PhoneNumber);

            if (!string.IsNullOrWhiteSpace(queueEntry.Party.PhoneNumber))
            {
                var twilioResult = await _twilioService.PlaceTableReadyCallAsync(
                    queueEntry.Party.PhoneNumber,
                    queueEntry.Party.PartyName,
                    CancellationToken.None);

                if (!twilioResult.Succeeded)
                {
                    _logger.LogError(
                        "❌ Failed to place Twilio call for party {PartyId}: {ErrorMessage}",
                        queueEntry.PartyId,
                        twilioResult.ErrorMessage);
                    TempData["WarningMessage"] = $"Party seated, but call failed: {twilioResult.ErrorMessage}";
                }
                else
                {
                    _logger.LogInformation(
                        "✅ Successfully placed Twilio call for party {PartyId} ({PartyName})",
                        queueEntry.PartyId,
                        queueEntry.Party.PartyName);
                    TempData["InfoMessage"] = $"Party seated and call placed successfully!";
                }
            }
            else
            {
                _logger.LogWarning("⚠️ Party {PartyId} has no phone number on file", queueEntry.PartyId);
                TempData["WarningMessage"] = "Party seated but has no phone number on file.";
            }

            TempData["SuccessMessage"] = "Party successfully seated.";
            return RedirectToPage("./Index");
        }
    }
}
