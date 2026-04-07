using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HOST.Pages.ManagerAccounts
{
    [Authorize(Roles = "Manager")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CreateModel(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public ManagerAccount ManagerAccount { get; set; } = new();

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            // 1️⃣ Create Identity user
            var identityUser = new IdentityUser
            {
                UserName = ManagerAccount.Email,
                Email = ManagerAccount.Email
            };

            var result = await _userManager.CreateAsync(identityUser, Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return Page();
            }

            // 2️⃣ Assign Manager role
            await _userManager.AddToRoleAsync(identityUser, "Manager");

            // 3️⃣ Create Employee row
            var employee = new Employee
            {
                Name = ManagerAccount.Email.Split('@')[0],
                DisplayName = ManagerAccount.Email.Split('@')[0],
                Email = ManagerAccount.Email,
                Role = "Manager",
                IdentityUserId = identityUser.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.Employees.Add(employee);

            // 4️⃣ Save ManagerAccount row
            ManagerAccount.CreatedAt = DateTime.UtcNow;
            _context.ManagerAccounts.Add(ManagerAccount);

            // 5️⃣ Commit everything
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
