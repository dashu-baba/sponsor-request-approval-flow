using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SponsorshipApproval.Application.Auth;

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
}
