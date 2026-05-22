using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SponsorshipApproval.Application.Auth;
using SponsorshipApproval.Application.Auth.Models;
using SponsorshipApproval.Infrastructure.Identity;
using SponsorshipApproval.Infrastructure.Persistence;

namespace SponsorshipApproval.Infrastructure.Auth;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    AppDbContext dbContext,
    IJwtTokenService jwtTokenService) : IAuthService
{
    public async Task<(LoginResponse Response, string RawRefreshToken, DateTimeOffset RefreshTokenExpiresAt)?> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email).ConfigureAwait(false);
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password).ConfigureAwait(false))
        {
            return null;
        }

        var role = await GetSingleRoleAsync(user, cancellationToken).ConfigureAwait(false);
        if (role is null)
        {
            return null;
        }

        return await IssueTokensAsync(user, role, cancellationToken).ConfigureAwait(false);
    }

    public async Task<(LoginResponse Response, string RawRefreshToken, DateTimeOffset RefreshTokenExpiresAt)?> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        var tokenHash = JwtTokenService.HashToken(refreshToken);
        var storedToken = await dbContext.RefreshTokens
            .SingleOrDefaultAsync(
                token => token.TokenHash == tokenHash && token.RevokedAt == null,
                cancellationToken)
            .ConfigureAwait(false);

        if (storedToken is null || storedToken.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return null;
        }

        var user = await userManager.FindByIdAsync(storedToken.UserId).ConfigureAwait(false);
        if (user is null)
        {
            return null;
        }

        var role = await GetSingleRoleAsync(user, cancellationToken).ConfigureAwait(false);
        if (role is null)
        {
            return null;
        }

        storedToken.RevokedAt = DateTimeOffset.UtcNow;

        var issued = await IssueTokensAsync(user, role, cancellationToken).ConfigureAwait(false);
        if (issued is null)
        {
            return null;
        }

        storedToken.ReplacedByTokenHash = JwtTokenService.HashToken(issued.Value.RawRefreshToken);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return issued;
    }

    public async Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var tokenHash = JwtTokenService.HashToken(refreshToken);
        var storedToken = await dbContext.RefreshTokens
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash && token.RevokedAt == null, cancellationToken)
            .ConfigureAwait(false);

        if (storedToken is null)
        {
            return;
        }

        storedToken.RevokedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<UserProfileResponse?> GetProfileAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var user = await userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (user is null)
        {
            return null;
        }

        var role = await GetSingleRoleAsync(user, cancellationToken).ConfigureAwait(false);
        if (role is null)
        {
            return null;
        }

        return new UserProfileResponse(
            user.Id,
            user.Email ?? string.Empty,
            user.DisplayName,
            user.Department,
            role);
    }

    private async Task<string?> GetSingleRoleAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
        if (roles.Count != 1)
        {
            return null;
        }

        _ = cancellationToken;
        return roles[0];
    }

    private async Task<(LoginResponse Response, string RawRefreshToken, DateTimeOffset RefreshTokenExpiresAt)?> IssueTokensAsync(
        ApplicationUser user,
        string role,
        CancellationToken cancellationToken)
    {
        var accessTokenExpiresAt = jwtTokenService.GetAccessTokenExpiry();
        var accessToken = jwtTokenService.CreateAccessToken(
            user.Id,
            user.Email ?? string.Empty,
            user.DisplayName,
            role);

        var (rawRefreshToken, refreshTokenHash) = jwtTokenService.CreateRefreshToken();
        var refreshTokenExpiresAt = jwtTokenService.GetRefreshTokenExpiry();

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = refreshTokenExpiresAt,
        });

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var response = new LoginResponse(accessToken, accessTokenExpiresAt, "Bearer");
        return (response, rawRefreshToken, refreshTokenExpiresAt);
    }
}
