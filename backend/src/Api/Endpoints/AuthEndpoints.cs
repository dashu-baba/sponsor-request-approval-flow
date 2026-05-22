using System.Security.Claims;
using FluentValidation;
using SponsorshipApproval.Application.Auth;
using SponsorshipApproval.Application.Auth.Models;

namespace SponsorshipApproval.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/auth").WithTags("Auth");

        auth.MapPost("/login", LoginAsync)
            .WithSummary("Sign in with email and password")
            .WithDescription("Returns a JWT access token and sets an httpOnly refresh token cookie.")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        auth.MapPost("/refresh", RefreshAsync)
            .WithSummary("Refresh the access token")
            .WithDescription("Uses the httpOnly refresh token cookie to issue a new access token.")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        auth.MapPost("/logout", LogoutAsync)
            .WithSummary("Sign out")
            .WithDescription("Revokes the refresh token cookie when present.")
            .Produces(StatusCodes.Status204NoContent);

        var me = app.MapGroup("/me")
            .RequireAuthorization()
            .WithTags("Auth");

        me.MapGet("", GetMeAsync)
            .WithSummary("Get the current user profile")
            .Produces<UserProfileResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        me.MapPut("/profile", UpdateProfileAsync)
            .WithSummary("Update the current user profile")
            .Produces<UserProfileResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        me.MapPut("/password", ChangePasswordAsync)
            .WithSummary("Change the current user password")
            .WithDescription("Rotates the refresh token cookie after a successful password change.")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        IAuthService authService,
        HttpContext httpContext,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return TypedResults.Problem(
                title: "Invalid login request",
                detail: "Email and password are required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var result = await authService.LoginAsync(request, cancellationToken).ConfigureAwait(false);
        if (result is null)
        {
            return TypedResults.Problem(
                title: "Unauthorized",
                detail: "Invalid email or password.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        AppendRefreshTokenCookie(httpContext.Response, result.Value.RawRefreshToken, result.Value.RefreshTokenExpiresAt, environment);
        return TypedResults.Ok(result.Value.Response);
    }

    private static async Task<IResult> RefreshAsync(
        IAuthService authService,
        HttpContext httpContext,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        if (!httpContext.Request.Cookies.TryGetValue(AuthConstants.RefreshTokenCookieName, out var refreshToken))
        {
            return TypedResults.Problem(
                title: "Unauthorized",
                detail: "Refresh token is missing.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await authService.RefreshAsync(refreshToken, cancellationToken).ConfigureAwait(false);
        if (result is null)
        {
            DeleteRefreshTokenCookie(httpContext.Response, environment);
            return TypedResults.Problem(
                title: "Unauthorized",
                detail: "Refresh token is invalid or expired.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        AppendRefreshTokenCookie(httpContext.Response, result.Value.RawRefreshToken, result.Value.RefreshTokenExpiresAt, environment);
        return TypedResults.Ok(result.Value.Response);
    }

    private static async Task<IResult> LogoutAsync(
        IAuthService authService,
        HttpContext httpContext,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        httpContext.Request.Cookies.TryGetValue(AuthConstants.RefreshTokenCookieName, out var refreshToken);
        await authService.LogoutAsync(refreshToken, cancellationToken).ConfigureAwait(false);
        DeleteRefreshTokenCookie(httpContext.Response, environment);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> GetMeAsync(
        IAuthService authService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var profile = await authService.GetProfileAsync(user, cancellationToken).ConfigureAwait(false);
        if (profile is null)
        {
            return TypedResults.Problem(
                title: "Unauthorized",
                detail: "The access token is invalid.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        return TypedResults.Ok(profile);
    }

    private static async Task<IResult> UpdateProfileAsync(
        UpdateProfileRequest request,
        IValidator<UpdateProfileRequest> validator,
        IAuthService authService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await authService.UpdateProfileAsync(user, request, cancellationToken).ConfigureAwait(false);
        if (result.Succeeded)
        {
            return TypedResults.Ok(result.Profile);
        }

        return result.FailureReason switch
        {
            UpdateProfileFailureReason.IdentityValidationFailed => TypedResults.Problem(
                title: "Invalid profile update request",
                detail: string.Join(' ', result.Errors ?? []),
                statusCode: StatusCodes.Status400BadRequest),
            UpdateProfileFailureReason.UnexpectedFailure => TypedResults.Problem(
                title: "Profile update failed",
                detail: "The profile could not be updated.",
                statusCode: StatusCodes.Status500InternalServerError),
            _ => TypedResults.Problem(
                title: "Unauthorized",
                detail: "The access token is invalid.",
                statusCode: StatusCodes.Status401Unauthorized),
        };
    }

    private static async Task<IResult> ChangePasswordAsync(
        ChangePasswordRequest request,
        IValidator<ChangePasswordRequest> validator,
        IAuthService authService,
        ClaimsPrincipal user,
        HttpContext httpContext,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await authService.ChangePasswordAsync(user, request, cancellationToken).ConfigureAwait(false);
        if (result.Succeeded)
        {
            AppendRefreshTokenCookie(
                httpContext.Response,
                result.RawRefreshToken!,
                result.RefreshTokenExpiresAt!.Value,
                environment);
            return TypedResults.Ok(result.Response);
        }

        return result.FailureReason switch
        {
            ChangePasswordFailureReason.WrongCurrentPassword => TypedResults.Problem(
                title: "Invalid password change request",
                detail: "Current password is incorrect.",
                statusCode: StatusCodes.Status400BadRequest),
            ChangePasswordFailureReason.PolicyViolation => TypedResults.Problem(
                title: "Invalid password change request",
                detail: string.Join(' ', result.PolicyErrors ?? []),
                statusCode: StatusCodes.Status400BadRequest),
            ChangePasswordFailureReason.SessionRefreshFailed => TypedResults.Problem(
                title: "Password change incomplete",
                detail: "Your password was updated, but the session could not be refreshed. Please sign in again.",
                statusCode: StatusCodes.Status500InternalServerError),
            _ => TypedResults.Problem(
                title: "Unauthorized",
                detail: "The access token is invalid.",
                statusCode: StatusCodes.Status401Unauthorized),
        };
    }

    private static void AppendRefreshTokenCookie(
        HttpResponse response,
        string refreshToken,
        DateTimeOffset expiresAt,
        IHostEnvironment environment)
    {
        response.Cookies.Append(
            AuthConstants.RefreshTokenCookieName,
            refreshToken,
            CreateRefreshCookieOptions(expiresAt, environment));
    }

    private static void DeleteRefreshTokenCookie(HttpResponse response, IHostEnvironment environment)
    {
        response.Cookies.Delete(
            AuthConstants.RefreshTokenCookieName,
            CreateRefreshCookieOptions(DateTimeOffset.UtcNow.AddDays(-1), environment));
    }

    private static CookieOptions CreateRefreshCookieOptions(DateTimeOffset expiresAt, IHostEnvironment environment) =>
        new()
        {
            HttpOnly = true,
            Secure = !environment.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Path = AuthConstants.RefreshTokenCookiePath,
            Expires = expiresAt,
        };
}
