using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages.Employees
{
    [Authorize(Roles = "Manager")]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Employee Employee { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var employee = await _context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null)
                return NotFound();

            Employee = employee;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == Employee.EmployeeId);

            if (employee == null)
                return NotFound();

            // Prevent deleting employees with seating history
            bool hasSeatings = await _context.Seatings.AnyAsync(s =>
                s.AssignedServerId == employee.EmployeeId ||
                s.SeatedByEmployeeId == employee.EmployeeId
            );

            if (hasSeatings)
            {
                TempData["ErrorMessage"] =
                    "This employee cannot be deleted because they have seating history.";
                return RedirectToPage("./Index");
            }

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Employee deleted successfully.";
            return RedirectToPage("./Index");
        }
    }
}
