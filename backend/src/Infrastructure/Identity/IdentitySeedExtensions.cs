using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
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
