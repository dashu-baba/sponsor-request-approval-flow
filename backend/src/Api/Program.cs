using SponsorshipApproval.Api.Endpoints;
using SponsorshipApproval.Infrastructure;
using SponsorshipApproval.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapAuthEndpoints();
app.MapSystemEndpoints();

await app.Services.MigrateAndSeedAsync().ConfigureAwait(false);

app.Run();

public partial class Program { }
