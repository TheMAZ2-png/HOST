using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages.Employees
{
    [Authorize(Roles = "Manager")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Employee> Employees { get; set; } = new List<Employee>();

        // ============================================================
        // GET — Load all employees
        // ============================================================
        public async Task OnGetAsync()
        {
            Employees = await _context.Employees
                .AsNoTracking()
                .OrderBy(e => e.Name)
                .ToListAsync();
        }
    }
}
