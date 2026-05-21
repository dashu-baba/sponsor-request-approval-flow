using System.Security.Claims;
using SponsorshipApproval.Application.Auth;
using SponsorshipApproval.Application.Auth.Models;

namespace SponsorshipApproval.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/auth").WithTags("Auth");

        auth.MapPost("/login", LoginAsync);
        auth.MapPost("/refresh", RefreshAsync);
        auth.MapPost("/logout", LogoutAsync);

        app.MapGet("/me", GetMeAsync)
            .RequireAuthorization()
            .WithTags("Auth");

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
