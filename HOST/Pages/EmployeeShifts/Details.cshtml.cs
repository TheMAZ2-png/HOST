using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages.EmployeeShifts
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

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
    }
}
