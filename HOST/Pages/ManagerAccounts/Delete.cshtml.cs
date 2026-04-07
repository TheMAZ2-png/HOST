using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages.ManagerAccounts
{
    [Authorize]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public DeleteModel(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public ManagerAccount ManagerAccount { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var account = await _context.ManagerAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ManagerId == id);

            if (account == null)
                return NotFound();

            ManagerAccount = account;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // 1️⃣ Load ManagerAccount
            var account = await _context.ManagerAccounts
                .FirstOrDefaultAsync(m => m.ManagerId == ManagerAccount.ManagerId);

            if (account == null)
                return NotFound();

            // 2️⃣ Find IdentityUser
            var identityUser = await _userManager.FindByEmailAsync(account.Email);

            // 3️⃣ Delete Employee row
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Email == account.Email);

            if (employee != null)
                _context.Employees.Remove(employee);

            // 4️⃣ Delete IdentityUser
            if (identityUser != null)
                await _userManager.DeleteAsync(identityUser);

            // 5️⃣ Delete ManagerAccount
            _context.ManagerAccounts.Remove(account);

            // 6️⃣ Save all changes
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
