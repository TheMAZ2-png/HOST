using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages.EmployeeRoles
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<EmployeeRole> EmployeeRoles { get; set; } = new List<EmployeeRole>();

        public async Task OnGetAsync()
        {
            EmployeeRoles = await _context.EmployeeRoles.AsNoTracking().ToListAsync();
        }
    }
}
