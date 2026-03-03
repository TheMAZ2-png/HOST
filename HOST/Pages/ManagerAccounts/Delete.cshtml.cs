using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages.ManagerAccounts
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
        public ManagerAccount ManagerAccount { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var account = await _context.ManagerAccounts.AsNoTracking().FirstOrDefaultAsync(m => m.ManagerId == id);
            if (account == null)
            {
                return NotFound();
            }

            ManagerAccount = account;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var account = await _context.ManagerAccounts.FindAsync(ManagerAccount.ManagerId);
            if (account == null)
            {
                return NotFound();
            }

            _context.ManagerAccounts.Remove(account);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
