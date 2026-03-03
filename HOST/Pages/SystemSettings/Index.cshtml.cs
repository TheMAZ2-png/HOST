using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages.SystemSettings
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<SystemSetting> SystemSettings { get; set; } = new List<SystemSetting>();

        public async Task OnGetAsync()
        {
            SystemSettings = await _context.SystemSettings.AsNoTracking().ToListAsync();
        }
    }
}
