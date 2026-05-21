using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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

    public static async Task SeedDatabaseAsync(this IServiceProvider services)
    {
        await services.SeedIdentityRolesAsync().ConfigureAwait(false);
        await services.SeedIdentityUsersAsync().ConfigureAwait(false);
        await services.SeedApplicationDataAsync().ConfigureAwait(false);
    }
}
