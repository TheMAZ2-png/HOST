using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages.SystemSettings
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
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var existing = await _context.SystemSettings.FirstOrDefaultAsync(s => s.SettingKey == SystemSetting.SettingKey);
            if (existing == null)
            {
                return NotFound();
            }

            existing.SettingValue = SystemSetting.SettingValue;
            existing.UpdatedByEmployeeId = SystemSetting.UpdatedByEmployeeId;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
