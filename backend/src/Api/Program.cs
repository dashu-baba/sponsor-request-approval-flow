using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Features;
using SponsorshipApproval.Api.Endpoints;
using SponsorshipApproval.Api.Infrastructure;
using SponsorshipApproval.Application.Attachments;
using SponsorshipApproval.Infrastructure;
using SponsorshipApproval.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: true));
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = AttachmentValidationConstants.MaxSizeBytes;
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = AttachmentValidationConstants.MaxSizeBytes;
    options.ValueLengthLimit = 1024 * 1024;
    options.MultipartHeadersLengthLimit = 16 * 1024;
});

builder.Services.AddHealthChecks();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapAuthEndpoints();
app.MapRequestEndpoints();
app.MapSponsorshipTypeEndpoints();
app.MapSystemEndpoints();

await app.Services.MigrateAndSeedAsync().ConfigureAwait(false);
await app.Services.EnsureObjectStorageAsync().ConfigureAwait(false);

app.Run();

public partial class Program { }
