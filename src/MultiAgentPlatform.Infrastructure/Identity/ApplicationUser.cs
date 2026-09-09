using Microsoft.AspNetCore.Identity;

namespace MultiAgentPlatform.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
}
