using System.Net.Sockets;
using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.Minio;
using Testcontainers.PostgreSql;

namespace SponsorshipApproval.Api.IntegrationTests.Infrastructure;

public sealed class PostgresWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    internal const string MinioUsername = "minioadmin";

    internal const string MinioPassword = "minioadmin123";

    internal const string MinioBucketName = "sponsorship-attachments-test";

    private PostgreSqlContainer? _postgres;

    private MinioContainer? _minio;

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

            _minio = new MinioBuilder("minio/minio:RELEASE.2025-09-07T16-13-09Z-cpuv1")
                .WithUsername(MinioUsername)
                .WithPassword(MinioPassword)
                .WithWaitStrategy(
                    Wait.ForUnixContainer()
                        .UntilExternalTcpPortIsAvailable(9000))
                .Build();

            await _postgres.StartAsync().ConfigureAwait(true);
            await _minio.StartAsync().ConfigureAwait(true);
        }
        catch (DockerUnavailableException exception)
        {
            Assert.Skip($"Docker is unavailable for Testcontainers: {exception.Message}");
        }
    }

    internal Task StopMinioContainerAsync(CancellationToken cancellationToken = default) =>
        _minio!.StopAsync(cancellationToken);

    internal async Task StartMinioContainerAsync(CancellationToken cancellationToken = default)
    {
        await _minio!.StartAsync(cancellationToken).ConfigureAwait(false);
        await WaitForMinioTcpAsync(cancellationToken).ConfigureAwait(false);
    }

    internal async Task EnsureMinioContainerRunningAsync(CancellationToken cancellationToken = default)
    {
        if (_minio!.State == DotNet.Testcontainers.Containers.TestcontainersStates.Running)
        {
            await WaitForMinioTcpAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        await StartMinioContainerAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task WaitForMinioTcpAsync(CancellationToken cancellationToken)
    {
        const int maxAttempts = 60;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var tcpClient = new TcpClient();
                await tcpClient
                    .ConnectAsync(_minio!.Hostname, _minio.GetMappedPublicPort(9000), cancellationToken)
                    .ConfigureAwait(false);

                return;
            }
            catch (Exception exception) when (exception is SocketException or InvalidOperationException)
            {
                if (attempt == maxAttempts - 1)
                {
                    throw new TimeoutException("MinIO container did not become reachable.", exception);
                }

                await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false);
            }
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", _postgres!.GetConnectionString());
        TestConfiguration.ApplyJwtSettings(builder);
        TestConfiguration.ApplyMinioSettings(builder, _minio!);
    }

    public override async ValueTask DisposeAsync()
    {
        if (_minio is not null)
        {
            await _minio.DisposeAsync().ConfigureAwait(true);
        }

        if (_postgres is not null)
        {
            await _postgres.DisposeAsync().ConfigureAwait(true);
        }

        await base.DisposeAsync().ConfigureAwait(true);
    }
}
