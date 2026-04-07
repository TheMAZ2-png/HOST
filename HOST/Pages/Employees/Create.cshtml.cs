using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HOST.Pages.Employees
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

        // Bindable fields (matching StaffCreationPage)
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

            // Prevent manager creation
            if (SelectedRole == "Manager")
            {
                ModelState.AddModelError(string.Empty, "Manager accounts cannot be created.");
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

            // Assign role
            await _userManager.AddToRoleAsync(user, SelectedRole);

            // Create Employee record
            var employee = new Employee
            {
                Name = $"{FirstName} {LastName}",
                DisplayName = FirstName,
                Email = Email,
                Phone = null,
                Role = SelectedRole,
                IdentityUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
