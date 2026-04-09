using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages.AdminInsight
{
    [Authorize(Roles = "Manager")]
    public class InsightModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public InsightModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // WAIT TIMES
        public int? BusiestHourET { get; set; }
        public string? BusiestDayOfWeek { get; set; }
        public List<(string Day, int AvgWait)> AvgWaitByDay { get; set; } = new();

        // PARTIES
        public int? AvgPartySize { get; set; }
        public List<(int PartySize, int AvgWait)> AvgWaitByPartySize { get; set; } = new();

        // TABLES
        public List<(int TableNumber, int Turnovers)> TableTurnover { get; set; } = new();

        // CUSTOMERS
        public int ReturningCustomersWeekly { get; set; }
        public int NewCustomersWeekly { get; set; }

        public async Task OnGet()
        {
            var eastern = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            var nowET = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, eastern);
            var weekStartET = nowET.AddDays(-7);

            // Pull completed parties
            var parties = await _context.Parties
                .Where(p => p.CompletedAt.HasValue && p.ActualWaitMinutes.HasValue)
                .ToListAsync();

            var partiesET = parties.Select(p => new
            {
                Party = p,
                CompletedET = TimeZoneInfo.ConvertTimeFromUtc(p.CompletedAt!.Value, eastern),
                CreatedET = TimeZoneInfo.ConvertTimeFromUtc(p.CreatedAt, eastern)
            }).ToList();

            // BUSIEST HOUR
            BusiestHourET = partiesET
                .GroupBy(p => p.CreatedET.Hour)
                .OrderByDescending(g => g.Count())
                .Select(g => (int?)g.Key)
                .FirstOrDefault();

            // BUSIEST DAY
            BusiestDayOfWeek = partiesET
                .GroupBy(p => p.CreatedET.DayOfWeek)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key.ToString())
                .FirstOrDefault();

            // AVG WAIT BY DAY
            AvgWaitByDay = partiesET
                .GroupBy(p => p.CompletedET.DayOfWeek)
                .Select(g => (
                    Day: g.Key.ToString(),
                    AvgWait: (int)Math.Round(g.Average(x => x.Party.ActualWaitMinutes!.Value))
                ))
                .OrderBy(x => x.Day)
                .ToList();

            // AVG PARTY SIZE
            if (parties.Any())
                AvgPartySize = (int)Math.Ceiling(parties.Average(p => p.PartySize));

            // AVG WAIT BY PARTY SIZE
            AvgWaitByPartySize = parties
                .GroupBy(p => p.PartySize)
                .Select(g => (
                    PartySize: g.Key,
                    AvgWait: (int)Math.Round(g.Average(x => x.ActualWaitMinutes!.Value))
                ))
                .OrderBy(x => x.PartySize)
                .ToList();

            // TABLE TURNOVER
            var seatings = await _context.Seatings
                .Include(s => s.RestaurantTable)
                .ToListAsync();

            TableTurnover = seatings
                .Where(s => s.SeatedAt >= weekStartET)
                .GroupBy(s => s.RestaurantTable.TableNumber)
                .Select(g => (
                    TableNumber: g.Key,
                    Turnovers: g.Count()
                ))
                .OrderBy(x => x.TableNumber)
                .ToList();

            // CUSTOMER ANALYTICS
            var weekParties = partiesET.Where(p => p.CompletedET >= weekStartET).ToList();

            var phoneGroups = weekParties
                .Where(p => !string.IsNullOrWhiteSpace(p.Party.PhoneNumber))
                .GroupBy(p => p.Party.PhoneNumber!)
                .ToList();

            ReturningCustomersWeekly = phoneGroups.Count(g =>
                _context.Parties.Any(p =>
                    p.PhoneNumber == g.Key &&
                    p.CompletedAt < weekStartET));

            NewCustomersWeekly = phoneGroups.Count() - ReturningCustomersWeekly;
        }
    }
}
