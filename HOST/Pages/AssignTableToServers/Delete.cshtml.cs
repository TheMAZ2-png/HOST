using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages.AssignTableToServers
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
        public AssignTableToServer Assignment { get; set; } = new AssignTableToServer { Name = string.Empty };

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var assignment = await _context.AssignTableToServers.AsNoTracking().FirstOrDefaultAsync(a => a.EmployeeID == id);
            if (assignment == null)
            {
                return NotFound();
            }

            Assignment = assignment;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var assignment = await _context.AssignTableToServers.FindAsync(Assignment.EmployeeID);
            if (assignment == null)
            {
                return NotFound();
            }

            _context.AssignTableToServers.Remove(assignment);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
