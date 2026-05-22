using DotNet.Testcontainers.Builders;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SponsorshipApproval.Domain.Requests;
using SponsorshipApproval.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace SponsorshipApproval.Api.IntegrationTests.Persistence;

public sealed class AppDbContextTests
{
    [Fact]
    public async Task Migration_should_apply_and_round_trip_sponsorship_request_with_expected_postgres_mapping()
    {
        PostgreSqlContainer? postgres = null;

        try
        {
            postgres = new PostgreSqlBuilder("postgres:17.9-alpine3.23")
                .WithDatabase("sponsorship_approval_tests")
                .WithUsername("sponsorship_app")
                .WithWaitStrategy(
                    Wait.ForUnixContainer()
                        .UntilExternalTcpPortIsAvailable(5432)
                        .UntilCommandIsCompleted("pg_isready -U sponsorship_app -d sponsorship_approval_tests"))
                .Build();

            await postgres.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        }
        catch (DockerUnavailableException exception)
        {
            Assert.Skip($"Docker is unavailable for Testcontainers: {exception.Message}");
        }

        try
        {
            var options = CreateOptions(postgres);

            using (var db = new AppDbContext(options))
            {
                await db.Database.MigrateAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

                db.SponsorshipTypes.Add(new SponsorshipType
                {
                    Id = 100L,
                    Name = "Conference",
                    Description = "Industry conference",
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                });

                db.SponsorshipRequests.Add(new SponsorshipRequest
                {
                    Id = 101L,
                    Title = "PostgreSQL Summit",
                    RequestorName = "Ada Lovelace",
                    RequestorId = "requestor-1",
                    Department = "Engineering",
                    SponsorshipTypeId = 100L,
                    EventName = "PG Summit",
                    EventDate = new DateOnly(2026, 7, 1),
                    RequestedAmount = 1250.75m,
                    Purpose = "Learn operational PostgreSQL practices.",
                    ExpectedBenefit = "Improve database reliability.",
                    Remarks = "Draft request",
                    CreatedAt = DateTimeOffset.UtcNow,
                });

                await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
            }

            using (var db = new AppDbContext(options))
            {
                var request = await db.SponsorshipRequests
                    .AsNoTracking()
                    .SingleAsync(TestContext.Current.CancellationToken)
                    .ConfigureAwait(true);

                request.Status.Should().Be(RequestStatus.Draft);
                request.RequestedAmount.Should().Be(1250.75m);

                var entity = db.Model.FindEntityType(typeof(SponsorshipRequest));
                entity.Should().NotBeNull();

                entity!.GetTableName().Should().Be("sponsorship_requests");
                entity.FindProperty(nameof(SponsorshipRequest.Version))!.IsConcurrencyToken.Should().BeTrue();
                entity.FindProperty(nameof(SponsorshipRequest.Version))!.GetColumnName().Should().Be("xmin");
                entity.FindProperty(nameof(SponsorshipRequest.RequestedAmount))!.GetColumnType().Should().Be("numeric(18,2)");
                entity.FindProperty(nameof(SponsorshipRequest.CreatedAt))!.GetColumnType().Should().Be("timestamp with time zone");
            }
        }
        finally
        {
            if (postgres is not null)
            {
                await postgres.DisposeAsync().ConfigureAwait(true);
            }
        }
    }

    private static DbContextOptions<AppDbContext> CreateOptions(PostgreSqlContainer postgres)
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(postgres.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;
    }
}
