using System.Text.Json;
using FluentAssertions;
using SponsorshipApproval.Api.IntegrationTests.Infrastructure;

namespace SponsorshipApproval.Api.IntegrationTests.Infrastructure;

public sealed class OpenApiDocumentationTests(PostgresWebApplicationFactory factory)
    : IClassFixture<PostgresWebApplicationFactory>
{
    [Fact]
    public async Task OpenApi_document_should_be_available_and_include_bearer_security()
    {
        using var client = factory.CreateClient();

        using var response = await client
            .GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.IsSuccessStatusCode.Should().BeTrue();

        await using var stream = await response.Content
            .ReadAsStreamAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        document.RootElement.GetProperty("components")
            .GetProperty("securitySchemes")
            .TryGetProperty("Bearer", out var bearerScheme)
            .Should()
            .BeTrue();

        bearerScheme.GetProperty("scheme").GetString().Should().Be("bearer");
        document.RootElement.GetProperty("paths").TryGetProperty("/auth/login", out _).Should().BeTrue();
        document.RootElement.GetProperty("paths").TryGetProperty("/requests", out _).Should().BeTrue();
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
}
