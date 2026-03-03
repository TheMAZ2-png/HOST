using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Linq;

namespace HOST.Pages
{
    [Authorize(Roles = "Manager")]
    public class ErrorModel : PageModel
    {
        private readonly ILogger<ErrorModel> _logger;
        private readonly ApplicationDbContext _context;

        public ErrorModel(ILogger<ErrorModel> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        public List<FailedLogin> FailedLoginAttempts { get; set; } = new();

        public void OnGet()
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

            _logger.LogError("Unhandled exception occurred. RequestId: {RequestId}", RequestId);

            FailedLoginAttempts = _context.FailedLogins
                .OrderByDescending(f => f.Timestamp)
                .Take(20)
                .ToList();
        }
    }
}
