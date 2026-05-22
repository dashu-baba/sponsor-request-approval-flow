using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace SponsorshipApproval.Api.Infrastructure.OpenApi;

internal static class OpenApiAuthConstants
{
    public const string BearerSchemeName = JwtBearerDefaults.AuthenticationScheme;
}
