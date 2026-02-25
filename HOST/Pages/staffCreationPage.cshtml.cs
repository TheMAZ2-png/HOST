using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HOST.Pages
{
    [AllowAnonymous]
    public class staffCreationPageModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public staffCreationPageModel(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [BindProperty] public string SelectedRole { get; set; } = string.Empty;
        [BindProperty] public string FirstName { get; set; } = string.Empty;
        [BindProperty] public string LastName { get; set; } = string.Empty;
        [BindProperty] public string Email { get; set; } = string.Empty;
        [BindProperty] public string Password { get; set; } = string.Empty;

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(SelectedRole))
            {
                ModelState.AddModelError(string.Empty, "Please fill out all fields and select a role.");
                return Page();
            }

            // Create Identity user
            var user = new IdentityUser
            {
                UserName = Email,
                Email = Email
            };

            var createResult = await _userManager.CreateAsync(user, Password);

            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return Page();
            }

            // Assign Identity role
            await _userManager.AddToRoleAsync(user, SelectedRole);

            // Create domain model
            if (SelectedRole == "Manager")
            {
                var manager = new ManagerAccount
                {
                    Email = Email,
                    PasswordHash = user.PasswordHash!,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ManagerAccounts.Add(manager);
            }
            else
            {
                var employee = new Employee
                {
                    Name = $"{FirstName} {LastName}",
                    Email = Email,
                    Role = SelectedRole,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = null
                };

                _context.Employees.Add(employee);
            }

            await _context.SaveChangesAsync();

            // Auto-login (RELIABLE METHOD)
            await _signInManager.PasswordSignInAsync(
                Email,
                Password,
                isPersistent: false,
                lockoutOnFailure: false
            );

            // Redirect to homePage instead of Index
            return RedirectToPage("/homePage");
        }
    }
}
