using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages.EmployeeShifts
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public EmployeeShift EmployeeShift { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var shift = await _context.EmployeeShifts.AsNoTracking().FirstOrDefaultAsync(s => s.ShiftId == id);
            if (shift == null)
            {
                return NotFound();
            }

            EmployeeShift = shift;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var existing = await _context.EmployeeShifts.FirstOrDefaultAsync(s => s.ShiftId == EmployeeShift.ShiftId);
            if (existing == null)
            {
                return NotFound();
            }

            existing.EmployeeId = EmployeeShift.EmployeeId;
            existing.ClockInAt = EmployeeShift.ClockInAt;
            existing.ClockOutAt = EmployeeShift.ClockOutAt;
            existing.ClockInByEmployeeId = EmployeeShift.ClockInByEmployeeId;
            existing.ClockOutByEmployeeId = EmployeeShift.ClockOutByEmployeeId;

            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
