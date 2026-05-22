using System.Security.Claims;
using SponsorshipApproval.Application.Auth.Models;

namespace SponsorshipApproval.Application.Auth;

public interface IAuthService
{
    Task<(LoginResponse Response, string RawRefreshToken, DateTimeOffset RefreshTokenExpiresAt)?> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);

    Task<(LoginResponse Response, string RawRefreshToken, DateTimeOffset RefreshTokenExpiresAt)?> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default);

    Task<UserProfileResponse?> GetProfileAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);

    Task<UpdateProfileResult> UpdateProfileAsync(
        ClaimsPrincipal principal,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default);

    Task<ChangePasswordResult> ChangePasswordAsync(
        ClaimsPrincipal principal,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserSummaryResponse>> ListUsersAsync(
        CancellationToken cancellationToken = default);

    Task<CreateUserResult> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default);
}
