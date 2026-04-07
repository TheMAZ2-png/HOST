using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly ApplicationDbContext _context;

        public LoginModel(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            ILogger<LoginModel> logger,
            ApplicationDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string ErrorMessage { get; set; }

        public void OnGet(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            if (!ModelState.IsValid)
                return Page();

            // ============================
            // SINGLE IDENTITY LOGIN FLOW
            // ============================
            var result = await _signInManager.PasswordSignInAsync(
                Input.Email,
                Input.Password,
                isPersistent: false,
                lockoutOnFailure: false
            );

            if (result.Succeeded)
                return RedirectToPage("/homePage");

            return await HandleFailedLoginAsync();
        }

        private async Task<IActionResult> HandleFailedLoginAsync()
        {
            _logger.LogWarning("Failed login attempt for user {Email}", Input.Email);

            _context.FailedLogins.Add(new FailedLogin
            {
                Username = Input.Email,
                Timestamp = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });

            await _context.SaveChangesAsync();

            ErrorMessage = "Invalid login attempt.";
            return Page();
        }

        public class InputModel
        {
            [System.ComponentModel.DataAnnotations.Required]
            public string Email { get; set; }

            [System.ComponentModel.DataAnnotations.Required]
            public string Password { get; set; }
        }
    }
}
