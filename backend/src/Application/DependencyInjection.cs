using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SponsorshipApproval.Application.Requests.Mapping;

namespace SponsorshipApproval.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddValidatorsFromAssembly(assembly);
        services.AddAutoMapper(configuration => configuration.AddProfile<RequestMappingProfile>());

        return services;
    }
}
