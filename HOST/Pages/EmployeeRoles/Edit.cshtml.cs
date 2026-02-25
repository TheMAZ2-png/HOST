using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages.EmployeeRoles
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
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var existing = await _context.EmployeeRoles.FirstOrDefaultAsync(er => er.EmployeeRoleId == EmployeeRole.EmployeeRoleId);
            if (existing == null)
            {
                return NotFound();
            }

            existing.EmployeeId = EmployeeRole.EmployeeId;
            existing.RoleId = EmployeeRole.RoleId;

            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
