using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SponsorshipApproval.Application.Auth;
using SponsorshipApproval.Infrastructure.Persistence.Seeding;

namespace SponsorshipApproval.Infrastructure.Identity;

public static class IdentitySeedExtensions
{
    public static async Task SeedIdentityRolesAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var roleName in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName).ConfigureAwait(false))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(roleName)).ConfigureAwait(false);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Failed to seed role '{roleName}': {string.Join(", ", result.Errors.Select(error => error.Description))}");
                }
            }
        }
    }

    public static async Task BootstrapAdminAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationUser>>();

        var email = config["Bootstrap__AdminEmail"];
        var password = config["Bootstrap__AdminPassword"];
        var displayName = config["Bootstrap__AdminDisplayName"] ?? "Admin";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        // Only bootstrap if no users exist yet — never overwrites an existing user.
        if (await userManager.Users.AnyAsync().ConfigureAwait(false))
        {
            return;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = displayName,
            EmailConfirmed = true,
        };

        var createResult = await userManager.CreateAsync(user, password).ConfigureAwait(false);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to bootstrap admin user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
        }

        var roleResult = await userManager.AddToRoleAsync(user, Roles.SystemAdmin).ConfigureAwait(false);
        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to assign SystemAdmin role to bootstrap user: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
        }

        logger.LogInformation("Bootstrap admin user created: {Email}", email);
    }

    public static async Task SeedIdentityUsersAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var seedUser in SeedData.Users.All)
        {
            var user = await userManager.FindByEmailAsync(seedUser.Email).ConfigureAwait(false);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    Id = seedUser.Id,
                    UserName = seedUser.Email,
                    Email = seedUser.Email,
                    DisplayName = seedUser.DisplayName,
                    Department = seedUser.Department,
                    EmailConfirmed = true,
                };

                var createResult = await userManager.CreateAsync(user, SeedData.DefaultPassword).ConfigureAwait(false);
                if (!createResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Failed to seed user '{seedUser.Email}': {string.Join(", ", createResult.Errors.Select(error => error.Description))}");
                }
            }

            if (await userManager.IsInRoleAsync(user, seedUser.Role).ConfigureAwait(false))
            {
                continue;
            }

            var roleResult = await userManager.AddToRoleAsync(user, seedUser.Role).ConfigureAwait(false);
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to assign role '{seedUser.Role}' to '{seedUser.Email}': {string.Join(", ", roleResult.Errors.Select(error => error.Description))}");
            }
        }
    }
}
