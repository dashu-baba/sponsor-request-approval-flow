using Microsoft.AspNetCore.Identity;

namespace SponsorshipApproval.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;

    public string? Department { get; set; }
}
