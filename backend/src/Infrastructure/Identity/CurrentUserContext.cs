using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using SponsorshipApproval.Application.Common;

namespace SponsorshipApproval.Infrastructure.Identity;

public sealed class CurrentUserContext(
    IHttpContextAccessor httpContextAccessor,
    UserManager<ApplicationUser> userManager) : ICurrentUserContext
{
    private ClaimsPrincipal User =>
        httpContextAccessor.HttpContext?.User
        ?? throw new InvalidOperationException("No authenticated user is available for the current request.");

    public string UserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? throw new InvalidOperationException("The access token is missing a subject claim.");

    public string DisplayName =>
        User.FindFirstValue(ClaimTypes.Name)
        ?? User.FindFirstValue(JwtRegisteredClaimNames.Name)
        ?? throw new InvalidOperationException("The access token is missing a display name claim.");

    public IReadOnlyList<string> Roles =>
        User.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList()
            .AsReadOnly();

    public async Task<string?> GetDepartmentAsync(CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(UserId).ConfigureAwait(false);
        return user?.Department;
    }
}
