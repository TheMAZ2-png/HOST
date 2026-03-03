using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages.EmployeeRoles
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
        public EmployeeRole EmployeeRole { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employeeRole = await _context.EmployeeRoles.AsNoTracking().FirstOrDefaultAsync(er => er.EmployeeRoleId == id);
            if (employeeRole == null)
            {
                return NotFound();
            }

            EmployeeRole = employeeRole;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var employeeRole = await _context.EmployeeRoles.FindAsync(EmployeeRole.EmployeeRoleId);
            if (employeeRole == null)
            {
                return NotFound();
            }

            _context.EmployeeRoles.Remove(employeeRole);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
