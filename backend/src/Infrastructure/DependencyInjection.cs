using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SponsorshipApproval.Application;
using SponsorshipApproval.Application.Auth;
using SponsorshipApproval.Application.Common;
using SponsorshipApproval.Application.Common.Behaviors;
using SponsorshipApproval.Infrastructure.Auth;
using SponsorshipApproval.Infrastructure.Identity;
using SponsorshipApproval.Infrastructure.Persistence;
using SponsorshipApproval.Infrastructure.Requests.Handlers;

namespace SponsorshipApproval.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        bool enableDevelopmentDiagnostics)
    {
        var connectionString = configuration.GetConnectionString("Default");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'Default' is not configured.");
        }

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
            options.UseSnakeCaseNamingConvention();
            options.EnableDetailedErrors(enableDevelopmentDiagnostics);
            options.EnableSensitiveDataLogging(enableDevelopmentDiagnostics);
        });

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerOptions>();

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer();

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthorizationPolicies.Requestor, policy => policy.RequireRole(Roles.Requestor))
            .AddPolicy(AuthorizationPolicies.Manager, policy => policy.RequireRole(Roles.Manager))
            .AddPolicy(AuthorizationPolicies.FinanceAdmin, policy => policy.RequireRole(Roles.FinanceAdmin))
            .AddPolicy(AuthorizationPolicies.SystemAdmin, policy => policy.RequireRole(Roles.SystemAdmin))
            .AddPolicy(AuthorizationPolicies.Approver, policy => policy.RequireRole(Roles.Manager, Roles.FinanceAdmin));

        services.AddHttpContextAccessor();
        services.AddApplication();
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(typeof(CreateRequestCommandHandler).Assembly);
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICurrentUserContext, CurrentUserContext>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        return services;
    }
}
