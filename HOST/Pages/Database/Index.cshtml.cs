using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages.Database
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public int EmployeeCount { get; set; }
        public int RoleCount { get; set; }
        public int EmployeeRoleCount { get; set; }
        public int EmployeeShiftCount { get; set; }
        public int SystemSettingCount { get; set; }
        public int PartyCount { get; set; }
        public int QueueEntryCount { get; set; }
        public int RestaurantTableCount { get; set; }
        public int SeatingCount { get; set; }
        public int AssignTableToServerCount { get; set; }

        public async Task OnGetAsync()
        {
            EmployeeCount = await _context.Employees.CountAsync();
            RoleCount = await _context.Roles.CountAsync();
            EmployeeRoleCount = await _context.EmployeeRoles.CountAsync();
            EmployeeShiftCount = await _context.EmployeeShifts.CountAsync();
            SystemSettingCount = await _context.SystemSettings.CountAsync();
            PartyCount = await _context.Parties.CountAsync();
            QueueEntryCount = await _context.QueueEntries.CountAsync();
            RestaurantTableCount = await _context.RestaurantTables.CountAsync();
            SeatingCount = await _context.Seatings.CountAsync();
            AssignTableToServerCount = await _context.AssignTableToServers.CountAsync();
        }
    }
}