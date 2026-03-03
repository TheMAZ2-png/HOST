using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages.EmployeeRoles
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

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
    }
}
