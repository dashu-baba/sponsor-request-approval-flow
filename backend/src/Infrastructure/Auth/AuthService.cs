using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SponsorshipApproval.Application.Audit;
using SponsorshipApproval.Application.Auth;
using SponsorshipApproval.Application.Auth.Models;
using SponsorshipApproval.Application.Common;
using SponsorshipApproval.Infrastructure.Identity;
using SponsorshipApproval.Infrastructure.Persistence;

namespace SponsorshipApproval.Infrastructure.Auth;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    AppDbContext dbContext,
    IJwtTokenService jwtTokenService,
    IAuditService auditService,
    ICurrentUserContext currentUser) : IAuthService
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

        return await IssueTokensAsync(user, role, recordLoginAudit: true, cancellationToken).ConfigureAwait(false);
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

        var issued = await IssueTokensAsync(user, role, recordLoginAudit: false, cancellationToken).ConfigureAwait(false);
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

        auditService.Record(new AuditRecord(
            storedToken.UserId,
            AuditActions.AuthLogout,
            AuditCategories.Auth,
            AuditResourceTypes.User,
            storedToken.UserId,
            Summary: "Signed out"));

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

    public async Task<UpdateProfileResult> UpdateProfileAsync(
        ClaimsPrincipal principal,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await FindUserFromPrincipalAsync(principal, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return UpdateProfileResult.Failed(UpdateProfileFailureReason.UserNotFound);
        }

        var changedFields = new List<string>();
        var trimmedDisplayName = request.DisplayName.Trim();
        var trimmedDepartment = string.IsNullOrWhiteSpace(request.Department) ? null : request.Department.Trim();

        if (!string.Equals(user.DisplayName, trimmedDisplayName, StringComparison.Ordinal))
        {
            changedFields.Add(nameof(ApplicationUser.DisplayName));
        }

        if (!string.Equals(user.Department, trimmedDepartment, StringComparison.Ordinal))
        {
            changedFields.Add(nameof(ApplicationUser.Department));
        }

        user.DisplayName = trimmedDisplayName;
        user.Department = trimmedDepartment;

        if (changedFields.Count == 0)
        {
            var role = await GetSingleRoleAsync(user, cancellationToken).ConfigureAwait(false);
            if (role is null)
            {
                return UpdateProfileResult.Failed(UpdateProfileFailureReason.UnexpectedFailure);
            }

            return UpdateProfileResult.Success(MapProfile(user, role));
        }

        var transaction = await dbContext.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var updateResult = await userManager.UpdateAsync(user).ConfigureAwait(false);
            if (!updateResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                var errors = updateResult.Errors.Select(error => error.Description).ToArray();
                return UpdateProfileResult.Failed(UpdateProfileFailureReason.IdentityValidationFailed, errors);
            }

            auditService.Record(new AuditRecord(
                user.Id,
                AuditActions.AuthProfileUpdated,
                AuditCategories.Auth,
                AuditResourceTypes.User,
                user.Id,
                Summary: $"Updated profile fields: {string.Join(", ", changedFields)}",
                Metadata: new Dictionary<string, object?> { ["changedFields"] = changedFields.ToArray() }));

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
        finally
        {
            await transaction.DisposeAsync().ConfigureAwait(false);
        }

        var updatedRole = await GetSingleRoleAsync(user, cancellationToken).ConfigureAwait(false);
        if (updatedRole is null)
        {
            return UpdateProfileResult.Failed(UpdateProfileFailureReason.UnexpectedFailure);
        }

        return UpdateProfileResult.Success(MapProfile(user, updatedRole));
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

        var issued = await RevokeAllRefreshTokensAndIssueAsync(user, cancellationToken).ConfigureAwait(false);
        if (issued.Succeeded)
        {
            return ChangePasswordResult.Success(
                issued.Response!,
                issued.RawRefreshToken!,
                issued.RefreshTokenExpiresAt!.Value);
        }

        return ChangePasswordResult.Failed(ChangePasswordFailureReason.SessionRefreshFailed);
    }

    public async Task<IReadOnlyList<UserSummaryResponse>> ListUsersAsync(
        CancellationToken cancellationToken = default)
    {
        var summaries = await dbContext.Users
            .AsNoTracking()
            .OrderBy(user => user.Email)
            .Select(user => new UserSummaryResponse(
                user.Id,
                user.Email ?? string.Empty,
                user.DisplayName,
                user.Department,
                (
                    from userRole in dbContext.UserRoles
                    where userRole.UserId == user.Id
                    join role in dbContext.Roles on userRole.RoleId equals role.Id
                    orderby role.Name
                    select role.Name ?? string.Empty
                ).FirstOrDefault() ?? "(none)"))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return summaries;
    }

    public async Task<CreateUserResult> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = request.DisplayName.Trim(),
            Department = string.IsNullOrWhiteSpace(request.Department) ? null : request.Department.Trim(),
            EmailConfirmed = true,
        };

        var transaction = await dbContext.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var createResult = await userManager.CreateAsync(user, request.InitialPassword).ConfigureAwait(false);
            if (!createResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);

                if (createResult.Errors.Any(error =>
                        error.Code is "DuplicateUserName" or "DuplicateEmail"))
                {
                    return CreateUserResult.Failed(CreateUserFailureReason.DuplicateEmail);
                }

                var policyErrors = createResult.Errors.Select(error => error.Description).ToArray();
                return CreateUserResult.Failed(CreateUserFailureReason.PolicyViolation, policyErrors);
            }

            var roleResult = await userManager.AddToRoleAsync(user, request.Role).ConfigureAwait(false);
            if (!roleResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return CreateUserResult.Failed(CreateUserFailureReason.RoleAssignmentFailed);
            }

            auditService.Record(new AuditRecord(
                currentUser.UserId,
                AuditActions.UserCreated,
                AuditCategories.User,
                AuditResourceTypes.User,
                user.Id,
                Summary: $"Created user {email}",
                Metadata: new Dictionary<string, object?>
                {
                    ["email"] = email,
                    ["role"] = request.Role,
                    ["displayName"] = user.DisplayName,
                }));

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
        finally
        {
            await transaction.DisposeAsync().ConfigureAwait(false);
        }

        return CreateUserResult.Success(MapSummary(user, request.Role));
    }

    private async Task<(bool Succeeded, LoginResponse? Response, string? RawRefreshToken, DateTimeOffset? RefreshTokenExpiresAt)> RevokeAllRefreshTokensAndIssueAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        var role = await GetSingleRoleAsync(user, cancellationToken).ConfigureAwait(false);
        if (role is null)
        {
            return (false, null, null, null);
        }

        var reloadedUser = await userManager.FindByIdAsync(user.Id).ConfigureAwait(false);
        if (reloadedUser is null)
        {
            return (false, null, null, null);
        }

        var transaction = await dbContext.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var now = DateTimeOffset.UtcNow;
            var activeTokens = await dbContext.RefreshTokens
                .Where(token => token.UserId == user.Id && token.RevokedAt == null)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var token in activeTokens)
            {
                token.RevokedAt = now;
            }

            var accessTokenExpiresAt = jwtTokenService.GetAccessTokenExpiry();
            var accessToken = jwtTokenService.CreateAccessToken(
                reloadedUser.Id,
                reloadedUser.Email ?? string.Empty,
                reloadedUser.DisplayName,
                role,
                reloadedUser.SecurityStamp ?? string.Empty);

            var (rawRefreshToken, refreshTokenHash) = jwtTokenService.CreateRefreshToken();
            var refreshTokenExpiresAt = jwtTokenService.GetRefreshTokenExpiry();

            dbContext.RefreshTokens.Add(new RefreshToken
            {
                UserId = reloadedUser.Id,
                TokenHash = refreshTokenHash,
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = refreshTokenExpiresAt,
            });

            auditService.Record(new AuditRecord(
                reloadedUser.Id,
                AuditActions.AuthPasswordChanged,
                AuditCategories.Auth,
                AuditResourceTypes.User,
                reloadedUser.Id,
                Summary: "Changed password"));

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            var response = new LoginResponse(accessToken, accessTokenExpiresAt, "Bearer");
            return (true, response, rawRefreshToken, refreshTokenExpiresAt);
        }
        finally
        {
            await transaction.DisposeAsync().ConfigureAwait(false);
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

    private static UserProfileResponse MapProfile(ApplicationUser user, string role)
    {
        var summary = MapSummary(user, role);
        return new UserProfileResponse(
            summary.Id,
            summary.Email,
            summary.DisplayName,
            summary.Department,
            summary.Role);
    }

    private static UserSummaryResponse MapSummary(ApplicationUser user, string role) =>
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
        bool recordLoginAudit,
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

        if (recordLoginAudit)
        {
            auditService.Record(new AuditRecord(
                user.Id,
                AuditActions.AuthLogin,
                AuditCategories.Auth,
                AuditResourceTypes.User,
                user.Id,
                Summary: "Signed in"));
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var response = new LoginResponse(accessToken, accessTokenExpiresAt, "Bearer");
        return (response, rawRefreshToken, refreshTokenExpiresAt);
    }
}
