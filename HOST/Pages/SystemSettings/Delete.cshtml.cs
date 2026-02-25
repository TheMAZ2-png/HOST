using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages.SystemSettings
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

        public async Task<IActionResult> OnPostAsync()
        {
            var setting = await _context.SystemSettings.FindAsync(SystemSetting.SettingKey);
            if (setting == null)
            {
                return NotFound();
            }

            _context.SystemSettings.Remove(setting);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
