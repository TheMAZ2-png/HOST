using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages.SystemSettings
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public SystemSetting SystemSetting { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var setting = await _context.SystemSettings.AsNoTracking().FirstOrDefaultAsync(s => s.SettingKey == id);
            if (setting == null)
            {
                return NotFound();
            }

            SystemSetting = setting;
            return Page();
        }
    }
}
