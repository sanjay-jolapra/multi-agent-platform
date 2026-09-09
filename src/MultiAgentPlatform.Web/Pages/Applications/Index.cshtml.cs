using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MultiAgentPlatform.Domain.Entities;
using MultiAgentPlatform.Infrastructure.Identity;
using MultiAgentPlatform.Infrastructure.Persistence;

namespace MultiAgentPlatform.Web.Pages.Applications;

[Authorize]
public class IndexModel : PageModel
{
    private readonly MainDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(MainDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public List<GeneratedApplication> Applications { get; set; } = new();

    public async Task OnGetAsync()
    {
        var userId = _userManager.GetUserId(User);

        Applications = await _db.GeneratedApplications
            .Where(a => a.OwnerUserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }
}
