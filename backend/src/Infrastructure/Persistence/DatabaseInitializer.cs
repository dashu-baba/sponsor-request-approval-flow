using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SponsorshipApproval.Application.Common.Storage;
using SponsorshipApproval.Infrastructure.Identity;
using SponsorshipApproval.Infrastructure.Persistence.Seeding;

namespace SponsorshipApproval.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task MigrateAndSeedAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync().ConfigureAwait(false);
        await services.SeedDatabaseAsync().ConfigureAwait(false);
    }

    public static async Task EnsureObjectStorageAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var objectStorage = scope.ServiceProvider.GetRequiredService<IObjectStorage>();
        await objectStorage.EnsureBucketExistsAsync(CancellationToken.None).ConfigureAwait(false);
    }

    public static async Task SeedDatabaseAsync(this IServiceProvider services)
    {
        // Roles are always required — auth breaks without them.
        await services.SeedIdentityRolesAsync().ConfigureAwait(false);

        // Demo users and sample data are only seeded in development/test environments.
        // Set SEED_DEMO_DATA=true in .env to enable.
        using var scope = services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var seedDemoData = config.GetValue<bool>("SEED_DEMO_DATA");

        if (seedDemoData)
        {
            await services.SeedIdentityUsersAsync().ConfigureAwait(false);
            await services.SeedApplicationDataAsync().ConfigureAwait(false);
            return;
        }

        // Production: create one admin user from Bootstrap__ env vars if no users exist yet.
        await services.BootstrapAdminAsync().ConfigureAwait(false);
    }
}
