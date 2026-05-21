using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SponsorshipApproval.Application.Common.Exceptions;

namespace SponsorshipApproval.Api.Infrastructure;

public sealed class GlobalExceptionHandler(IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail, errors) = MapException(exception);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = $"https://httpstatuses.com/{statusCode}",
        };

        if (errors is not null)
        {
            problemDetails.Extensions["errors"] = errors;
        }

        if (environment.IsDevelopment() && statusCode >= StatusCodes.Status500InternalServerError)
        {
            problemDetails.Extensions["exception"] = exception.ToString();
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken).ConfigureAwait(false);

        return true;
    }

    private static (int StatusCode, string Title, string Detail, IDictionary<string, string[]>? Errors) MapException(
        Exception exception) =>
        exception switch
        {
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                "Validation failed",
                "One or more validation errors occurred.",
                validationException.Errors
                    .GroupBy(failure => failure.PropertyName)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray())),
            NotFoundException notFound => (
                StatusCodes.Status404NotFound,
                "Not found",
                notFound.Message,
                null),
            ForbiddenException forbidden => (
                StatusCodes.Status403Forbidden,
                "Forbidden",
                forbidden.Message,
                null),
            ConflictException conflict => (
                StatusCodes.Status409Conflict,
                "Conflict",
                conflict.Message,
                null),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred",
                "An unexpected error occurred while processing the request.",
                null),
        };
}
