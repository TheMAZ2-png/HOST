using HOST.Data;
using HOST.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HOST.Pages.Parties
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
        public Party Party { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var party = await _context.Parties.AsNoTracking().FirstOrDefaultAsync(p => p.PartyId == id);
            if (party == null)
            {
                return NotFound();
            }

            Party = party;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var party = await _context.Parties.FindAsync(Party.PartyId);
            if (party == null)
            {
                return NotFound();
            }

            _context.Parties.Remove(party);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
