using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StatisticsService.Api;
using StatisticsService.Api.Integration;
using StatisticsService.Application;
using StatisticsService.Infrastructure;
using StatisticsService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<StatisticsApplicationService>();
builder.Services.AddHealthChecks();
builder.Services.AddAuthorization();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<MatchCompletedConsumer>();
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"] ?? "rabbitmq", builder.Configuration["RabbitMq:VirtualHost"] ?? "/", h =>
        {
            h.Username(builder.Configuration["RabbitMq:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMq:Password"] ?? "guest");
        });
        cfg.ReceiveEndpoint("statistics-match-completed", endpoint =>
        {
            endpoint.UseMessageRetry(retry => retry.Interval(5, TimeSpan.FromSeconds(1)));
            endpoint.ConfigureConsumer<MatchCompletedConsumer>(context);
        });
    });
});

var jwt = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
if (jwt.Secret.Length < 32) throw new InvalidOperationException("Jwt:Secret must contain at least 32 characters.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
        ValidIssuer = jwt.Issuer, ValidAudience = jwt.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)), ClockSkew = TimeSpan.FromSeconds(30)
    };
});

var app = builder.Build();
app.UseAuthentication(); app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapGet("/api/status", () => Results.Ok(new { service = "StatisticsService", status = "m8-stats-phase3" })).RequireAuthorization();
app.MapStatisticsEndpoints();
await using (var scope = app.Services.CreateAsyncScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<StatisticsDbContext>>();
    await using var db = await factory.CreateDbContextAsync();
    await db.Database.MigrateAsync();
}
app.Run();
