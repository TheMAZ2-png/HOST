using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages.Employees
{
    [Authorize(Roles = "Manager")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public IndexModel(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IList<Employee> Employees { get; set; } = new List<Employee>();

        // ============================================================
        // GET — Load all employees except Managers
        // ============================================================
        public async Task OnGetAsync()
        {
            Employees = await _context.Employees
                .Where(e => e.Role != "Manager")
                .AsNoTracking()
                .OrderBy(e => e.Name)
                .ToListAsync();
        }

        // ============================================================
        // POST — DELETE ALL EMPLOYEES (Hard Delete)
        // ============================================================
        public async Task<IActionResult> OnPostDeleteAllAsync()
        {
            // 1️⃣ Delete all seatings first (to avoid FK constraint errors)
            var seatings = await _context.Seatings.ToListAsync();
            _context.Seatings.RemoveRange(seatings);
            await _context.SaveChangesAsync();

            // 2️⃣ Load all employees except managers
            var employees = await _context.Employees
                .Where(e => e.Role != "Manager")
                .ToListAsync();

            foreach (var emp in employees)
            {
                // Delete Identity user
                if (!string.IsNullOrEmpty(emp.IdentityUserId))
                {
                    var identityUser = await _userManager.FindByIdAsync(emp.IdentityUserId);
                    if (identityUser != null)
                        await _userManager.DeleteAsync(identityUser);
                }

                // Delete Employee record
                _context.Employees.Remove(emp);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "All employees have been deleted.";
            return RedirectToPage();
        }
    }
}
