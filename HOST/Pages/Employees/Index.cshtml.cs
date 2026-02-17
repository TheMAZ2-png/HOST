using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

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

        public IList<Employee> Employees { get; set; }

        public void OnGet()
        {
            Employees = _context.Employees.ToList();
        }
    }
}