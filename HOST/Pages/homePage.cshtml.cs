using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages
{
    [Authorize(Roles = "Manager,Host,Server")]
    public class homePageModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public homePageModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public int? TodayAverageWait { get; set; }
        public int? WeeklyAverageWait { get; set; }
        public List<SlowHourResult> SlowestHours { get; set; } = new();

        public class SlowHourResult
        {
            public int Hour { get; set; }
            public int AverageWait { get; set; }
        }

        public async Task OnGetAsync()
        {
            await LoadAnalyticsAsync();
        }

        private async Task LoadAnalyticsAsync()
        {
            var easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

            // Convert "now" to ET
            var nowET = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, easternZone);
            var todayET = nowET.Date;
            var weekStartET = nowET.AddDays(-7);

            // Pull completed parties from DB (UTC timestamps)
            var completed = await _context.Parties
                .Where(p =>
                    p.Status == "Completed" &&
                    p.CompletedAt.HasValue &&
                    p.ActualWaitMinutes.HasValue)
                .ToListAsync(); // ⭐ Pull into memory BEFORE timezone conversion

            // Convert timestamps to ET in memory
            var completedET = completed
                .Select(p => new
                {
                    Party = p,
                    CompletedAtET = TimeZoneInfo.ConvertTimeFromUtc(p.CompletedAt!.Value, easternZone)
                })
                .ToList();

            // ⭐ TODAY AVERAGE WAIT (ET)
            var todayList = completedET
                .Where(x => x.CompletedAtET.Date == todayET)
                .Select(x => x.Party.ActualWaitMinutes!.Value)
                .ToList();

            TodayAverageWait = todayList.Any()
                ? (int?)Math.Round(todayList.Average())
                : null;

            // ⭐ WEEKLY AVERAGE WAIT (ET)
            var weekList = completedET
                .Where(x => x.CompletedAtET >= weekStartET)
                .Select(x => x.Party.ActualWaitMinutes!.Value)
                .ToList();

            WeeklyAverageWait = weekList.Any()
                ? (int?)Math.Round(weekList.Average())
                : null;

            // ⭐ SLOWEST HOURS (ET)
            SlowestHours = completedET
                .Where(x => x.CompletedAtET >= weekStartET)
                .GroupBy(x => x.CompletedAtET.Hour)
                .Select(g => new SlowHourResult
                {
                    Hour = g.Key,
                    AverageWait = (int)Math.Round(g.Average(x => x.Party.ActualWaitMinutes!.Value))
                })
                .OrderByDescending(x => x.AverageWait)
                .Take(3)
                .ToList();
        }
    }
}
