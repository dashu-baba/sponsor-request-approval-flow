using FluentValidation;
using SponsorshipApproval.Application.Auth;
using SponsorshipApproval.Application.Auth.Models;

namespace SponsorshipApproval.Api.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var users = app.MapGroup("/users")
            .RequireAuthorization(AuthorizationPolicies.SystemAdmin)
            .WithTags("Users");

        users.MapGet("/", ListAsync);
        users.MapPost("/", CreateAsync);

        return app;
    }

    private static async Task<IResult> ListAsync(
        IAuthService authService,
        CancellationToken cancellationToken)
    {
        var result = await authService.ListUsersAsync(cancellationToken).ConfigureAwait(false);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> CreateAsync(
        CreateUserRequest request,
        IValidator<CreateUserRequest> validator,
        IAuthService authService,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await authService.CreateUserAsync(request, cancellationToken).ConfigureAwait(false);
        if (result.Succeeded)
        {
            return TypedResults.Created($"/users/{result.User!.Id}", result.User);
        }

        return result.FailureReason switch
        {
            CreateUserFailureReason.DuplicateEmail => TypedResults.Problem(
                title: "Conflict",
                detail: "A user with this email already exists.",
                statusCode: StatusCodes.Status409Conflict),
            CreateUserFailureReason.PolicyViolation => TypedResults.Problem(
                title: "Invalid user creation request",
                detail: string.Join(' ', result.PolicyErrors ?? []),
                statusCode: StatusCodes.Status400BadRequest),
            CreateUserFailureReason.RoleAssignmentFailed => TypedResults.Problem(
                title: "User creation failed",
                detail: "The user was created but the role could not be assigned.",
                statusCode: StatusCodes.Status500InternalServerError),
            _ => TypedResults.Problem(
                title: "User creation failed",
                detail: "The user could not be created.",
                statusCode: StatusCodes.Status500InternalServerError),
        };
    }
}
