using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace SponsorshipApproval.Api.IntegrationTests.Infrastructure;

public sealed class OpenApiDocumentationTests(PostgresWebApplicationFactory factory)
    : IClassFixture<PostgresWebApplicationFactory>
{
    [Fact]
    public async Task OpenApi_document_should_be_available_and_include_bearer_security()
    {
        using var document = await LoadOpenApiDocumentAsync().ConfigureAwait(true);

        document.RootElement.GetProperty("components")
            .GetProperty("securitySchemes")
            .TryGetProperty(JwtBearerDefaults.AuthenticationScheme, out var bearerScheme)
            .Should()
            .BeTrue();

        bearerScheme.GetProperty("scheme").GetString().Should().Be("bearer");
        document.RootElement.GetProperty("paths").TryGetProperty("/auth/login", out _).Should().BeTrue();
        document.RootElement.GetProperty("paths").TryGetProperty("/requests", out _).Should().BeTrue();
    }

    [Fact]
    public async Task OpenApi_document_should_require_bearer_on_authorized_operations_only()
    {
        using var document = await LoadOpenApiDocumentAsync().ConfigureAwait(true);
        var paths = document.RootElement.GetProperty("paths");

        OperationRequiresBearer(paths, "/requests", "get").Should().BeTrue();
        OperationRequiresBearer(paths, "/me", "get").Should().BeTrue();
        OperationRequiresBearer(paths, "/auth/login", "post").Should().BeFalse();
        OperationRequiresBearer(paths, "/auth/refresh", "post").Should().BeFalse();
    }

    [Fact]
    public async Task OpenApi_document_should_include_health_endpoints_without_bearer_auth()
    {
        using var document = await LoadOpenApiDocumentAsync().ConfigureAwait(true);
        var paths = document.RootElement.GetProperty("paths");

        foreach (var path in new[] { "/health/live", "/health/ready", "/health", "/system/ping" })
        {
            paths.TryGetProperty(path, out var pathItem).Should().BeTrue($"expected OpenAPI path {path}");
            pathItem.TryGetProperty("get", out var operation).Should().BeTrue();
            OperationRequiresBearer(paths, path, "get").Should().BeFalse();
        }
    }

    [Fact]
    public async Task Scalar_ui_should_be_reachable()
    {
        using var client = factory.CreateClient();

        using var response = await client
            .GetAsync("/scalar/v1", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.IsSuccessStatusCode.Should().BeTrue();
        response.Content.Headers.ContentType?.MediaType.Should().Contain("text/html");
    }

    private async Task<JsonDocument> LoadOpenApiDocumentAsync()
    {
        using var client = factory.CreateClient();

        using var response = await client
            .GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.IsSuccessStatusCode.Should().BeTrue();

        await using var stream = await response.Content
            .ReadAsStreamAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        return await JsonDocument.ParseAsync(stream, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
    }

    private static bool OperationRequiresBearer(JsonElement paths, string path, string method)
    {
        if (!paths.TryGetProperty(path, out var pathItem))
        {
            return false;
        }

        if (!pathItem.TryGetProperty(method, out var operation))
        {
            return false;
        }

        if (!operation.TryGetProperty("security", out var securityRequirements))
        {
            return false;
        }

        foreach (var requirement in securityRequirements.EnumerateArray())
        {
            if (requirement.TryGetProperty(JwtBearerDefaults.AuthenticationScheme, out _))
            {
                return true;
            }
        }

        return false;
    }
}
