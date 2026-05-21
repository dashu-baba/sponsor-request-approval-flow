using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SponsorshipApproval.Application.Auth;
using SponsorshipApproval.Infrastructure.Auth;

namespace SponsorshipApproval.Application.Tests.Auth;

public sealed class JwtTokenServiceTests
{
    private static JwtTokenService CreateService() =>
        new(Microsoft.Extensions.Options.Options.Create(new JwtOptions
        {
            Issuer = "unit-test-issuer",
            Audience = "unit-test-audience",
            SigningKey = "unit-test-signing-key-at-least-32-characters",
            AccessTokenLifetimeMinutes = 15,
            RefreshTokenLifetimeDays = 7,
        }));

    [Fact]
    public void CreateAccessToken_should_include_role_claim_and_validate()
    {
        var service = CreateService();
        var token = service.CreateAccessToken(
            "user-123",
            "user@example.com",
            "Test User",
            Roles.Manager);

        token.Should().NotBeNullOrWhiteSpace();

        var handler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "unit-test-issuer",
            ValidateAudience = true,
            ValidAudience = "unit-test-audience",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("unit-test-signing-key-at-least-32-characters")),
            RoleClaimType = ClaimTypes.Role,
        };

        var principal = handler.ValidateToken(token, validationParameters, out _);
        principal.FindFirstValue(ClaimTypes.Role).Should().Be(Roles.Manager);
        principal.FindFirstValue(ClaimTypes.NameIdentifier).Should().Be("user-123");
    }

    [Fact]
    public void CreateRefreshToken_should_return_unique_hashed_values()
    {
        var service = CreateService();

        var first = service.CreateRefreshToken();
        var second = service.CreateRefreshToken();

        first.RawToken.Should().NotBe(second.RawToken);
        first.TokenHash.Should().NotBe(first.RawToken);
        first.TokenHash.Should().NotBe(second.TokenHash);
    }
}

public sealed class AuthorizationPolicyTests
{
    [Fact]
    public void Role_and_policy_names_should_align()
    {
        AuthorizationPolicies.Requestor.Should().Be(Roles.Requestor);
        AuthorizationPolicies.Manager.Should().Be(Roles.Manager);
        AuthorizationPolicies.FinanceAdmin.Should().Be(Roles.FinanceAdmin);
        AuthorizationPolicies.SystemAdmin.Should().Be(Roles.SystemAdmin);
    }

    [Fact]
    public async Task Service_provider_should_register_role_policies()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorizationBuilder()
            .AddPolicy(AuthorizationPolicies.Requestor, policy => policy.RequireRole(Roles.Requestor))
            .AddPolicy(AuthorizationPolicies.Manager, policy => policy.RequireRole(Roles.Manager))
            .AddPolicy(AuthorizationPolicies.FinanceAdmin, policy => policy.RequireRole(Roles.FinanceAdmin))
            .AddPolicy(AuthorizationPolicies.SystemAdmin, policy => policy.RequireRole(Roles.SystemAdmin));

        using var provider = services.BuildServiceProvider();
        var policyProvider = provider.GetRequiredService<IAuthorizationPolicyProvider>();

        var policy = await policyProvider.GetPolicyAsync(AuthorizationPolicies.SystemAdmin);
        policy!.Requirements.Should().NotBeEmpty();
    }
}
