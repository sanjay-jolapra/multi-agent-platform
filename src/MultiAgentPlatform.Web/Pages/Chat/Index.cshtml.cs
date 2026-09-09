using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MultiAgentPlatform.Domain.Entities;
using MultiAgentPlatform.Infrastructure.Identity;
using MultiAgentPlatform.Infrastructure.Persistence;

namespace MultiAgentPlatform.Web.Pages.Chat;

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

    public Guid ConversationId { get; set; }

    public List<ChatMessage> History { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? conversationId)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null)
        {
            return Forbid();
        }

        Conversation? conversation = null;

        if (conversationId is not null)
        {
            conversation = await _db.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId);
        }

        if (conversation is null)
        {
            conversation = new Conversation
            {
                UserId = userId
            };
            _db.Conversations.Add(conversation);
            await _db.SaveChangesAsync();
        }

        ConversationId = conversation.Id;
        History = await _db.ChatMessages
            .Where(m => m.ConversationId == conversation.Id)
            .OrderBy(m => m.Timestamp)
            .ToListAsync();

        return Page();
    }
}
