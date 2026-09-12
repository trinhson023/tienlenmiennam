using IdentityService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks();

var app = builder.Build();
app.MapHealthChecks("/health");
app.MapGet("/api/status", () => Results.Ok(new { service = "IdentityService", status = "foundation-ready" }));
app.Run();
