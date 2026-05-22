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
        var user = await FindUserFromPrincipalAsync(principal, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return null;
        }

        var role = await GetSingleRoleAsync(user, cancellationToken).ConfigureAwait(false);
        if (role is null)
        {
            return null;
        }

        return MapProfile(user, role);
    }

    public async Task<UserProfileResponse?> UpdateProfileAsync(
        ClaimsPrincipal principal,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await FindUserFromPrincipalAsync(principal, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return null;
        }

        user.DisplayName = request.DisplayName.Trim();
        user.Department = string.IsNullOrWhiteSpace(request.Department) ? null : request.Department.Trim();

        var updateResult = await userManager.UpdateAsync(user).ConfigureAwait(false);
        if (!updateResult.Succeeded)
        {
            return null;
        }

        var role = await GetSingleRoleAsync(user, cancellationToken).ConfigureAwait(false);
        if (role is null)
        {
            return null;
        }

        return MapProfile(user, role);
    }

    public async Task<ChangePasswordResult> ChangePasswordAsync(
        ClaimsPrincipal principal,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await FindUserFromPrincipalAsync(principal, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return ChangePasswordResult.Failed(ChangePasswordFailureReason.UserNotFound);
        }

        var changeResult = await userManager
            .ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword)
            .ConfigureAwait(false);

        if (!changeResult.Succeeded)
        {
            if (changeResult.Errors.Any(error => error.Code == "PasswordMismatch"))
            {
                return ChangePasswordResult.Failed(ChangePasswordFailureReason.WrongCurrentPassword);
            }

            var policyErrors = changeResult.Errors.Select(error => error.Description).ToArray();
            return ChangePasswordResult.Failed(ChangePasswordFailureReason.PolicyViolation, policyErrors);
        }

        await RevokeAllRefreshTokensAsync(user.Id, cancellationToken).ConfigureAwait(false);

        var role = await GetSingleRoleAsync(user, cancellationToken).ConfigureAwait(false);
        if (role is null)
        {
            return ChangePasswordResult.Failed(ChangePasswordFailureReason.UserNotFound);
        }

        var reloadedUser = await userManager.FindByIdAsync(user.Id).ConfigureAwait(false);
        if (reloadedUser is null)
        {
            return ChangePasswordResult.Failed(ChangePasswordFailureReason.UserNotFound);
        }

        var issued = await IssueTokensAsync(reloadedUser, role, cancellationToken).ConfigureAwait(false);
        if (issued is null)
        {
            return ChangePasswordResult.Failed(ChangePasswordFailureReason.UserNotFound);
        }

        return ChangePasswordResult.Success(
            issued.Value.Response,
            issued.Value.RawRefreshToken,
            issued.Value.RefreshTokenExpiresAt);
    }

    private async Task RevokeAllRefreshTokensAsync(string userId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var activeTokens = await dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var token in activeTokens)
        {
            token.RevokedAt = now;
        }

        if (activeTokens.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<ApplicationUser?> FindUserFromPrincipalAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        _ = cancellationToken;
        return await userManager.FindByIdAsync(userId).ConfigureAwait(false);
    }

    private static UserProfileResponse MapProfile(ApplicationUser user, string role) =>
        new(
            user.Id,
            user.Email ?? string.Empty,
            user.DisplayName,
            user.Department,
            role);

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
            role,
            user.SecurityStamp ?? string.Empty);

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
