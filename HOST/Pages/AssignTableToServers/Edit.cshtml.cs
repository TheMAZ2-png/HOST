using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages.AssignTableToServers
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
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var existing = await _context.AssignTableToServers.FirstOrDefaultAsync(a => a.EmployeeID == Assignment.EmployeeID);
            if (existing == null)
            {
                return NotFound();
            }

            existing.Name = Assignment.Name;
            existing.DisplayName = Assignment.DisplayName;
            existing.Email = Assignment.Email;
            existing.Phone = Assignment.Phone;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
