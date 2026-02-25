using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages.EmployeeShifts
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<EmployeeShift> EmployeeShifts { get; set; } = new List<EmployeeShift>();

        public async Task OnGetAsync()
        {
            EmployeeShifts = await _context.EmployeeShifts.AsNoTracking().ToListAsync();
        }
    }
}
