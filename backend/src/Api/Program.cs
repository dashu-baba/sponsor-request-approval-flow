using SponsorshipApproval.Infrastructure;

// Entrypoint — full DI wiring and middleware configuration come in later tasks.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks("/health");

app.Run();

// Expose the generated Program class for WebApplicationFactory in integration tests.
public partial class Program { }
