using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace SponsorshipApproval.Api.IntegrationTests.Infrastructure;

public sealed class PostgresWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;

    public async ValueTask InitializeAsync()
    {
        try
        {
            _postgres = new PostgreSqlBuilder("postgres:17.9-alpine3.23")
                .WithDatabase("sponsorship_approval_tests")
                .WithUsername("sponsorship_app")
                .WithWaitStrategy(
                    Wait.ForUnixContainer()
                        .UntilExternalTcpPortIsAvailable(5432)
                        .UntilCommandIsCompleted("pg_isready -U sponsorship_app -d sponsorship_approval_tests"))
                .Build();

            await _postgres.StartAsync().ConfigureAwait(true);
        }
        catch (DockerUnavailableException exception)
        {
            Assert.Skip($"Docker is unavailable for Testcontainers: {exception.Message}");
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", _postgres!.GetConnectionString());
        TestConfiguration.ApplyJwtSettings(builder);
    }

    public override async ValueTask DisposeAsync()
    {
        if (_postgres is not null)
        {
            await _postgres.DisposeAsync().ConfigureAwait(true);
        }

        await base.DisposeAsync().ConfigureAwait(true);
    }
}
