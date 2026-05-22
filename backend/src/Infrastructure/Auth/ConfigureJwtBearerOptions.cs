using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SponsorshipApproval.Application.Auth;
using SponsorshipApproval.Infrastructure.Identity;

namespace SponsorshipApproval.Infrastructure.Auth;

internal sealed class ConfigureJwtBearerOptions(IOptions<JwtOptions> jwtOptions)
    : IConfigureNamedOptions<JwtBearerOptions>
{
    public void Configure(string? name, JwtBearerOptions options) => Configure(options);

    public void Configure(JwtBearerOptions options)
    {
        var jwt = jwtOptions.Value;

        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            NameClaimType = System.Security.Claims.ClaimTypes.Name,
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = ValidateSecurityStampAsync,
        };
    }

    private static async Task ValidateSecurityStampAsync(TokenValidatedContext context)
    {
        var userId = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var stampClaim = context.Principal?.FindFirstValue(AuthConstants.SecurityStampClaimType);

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(stampClaim))
        {
            context.Fail("The access token is invalid.");
            return;
        }

        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(userId).ConfigureAwait(false);

        if (user is null || !string.Equals(user.SecurityStamp, stampClaim, StringComparison.Ordinal))
        {
            context.Fail("The access token is invalid.");
        }
    }
}
