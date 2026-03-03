using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HOST.Pages.EmployeeShifts
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public EmployeeShift EmployeeShift { get; set; } = new();

        public IActionResult OnGet()
        {
            if (EmployeeShift.ClockInAt == default)
            {
                EmployeeShift.ClockInAt = DateTime.UtcNow;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.EmployeeShifts.Add(EmployeeShift);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
