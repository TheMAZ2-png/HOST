using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages.EmployeeShifts
{
    [Authorize]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
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
            var shift = await _context.EmployeeShifts.FindAsync(EmployeeShift.ShiftId);
            if (shift == null)
            {
                return NotFound();
            }

            _context.EmployeeShifts.Remove(shift);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
