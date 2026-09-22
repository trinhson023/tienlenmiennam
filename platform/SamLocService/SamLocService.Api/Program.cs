using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SamLocService.Api;
using SamLocService.Api.Hubs;
using SamLocService.Api.Integration;
using SamLocService.Application.Matches;
using SamLocService.Domain.Cards;
using SamLocService.Domain.Rules;
using SamLocService.Infrastructure;
using SamLocService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks();
builder.Services.AddSignalR(options => options.EnableDetailedErrors = builder.Environment.IsDevelopment());
builder.Services.AddAuthorization();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.AllowAnyHeader().AllowAnyMethod().SetIsOriginAllowed(_ => true).AllowCredentials()));

builder.Services.AddMassTransit(x => x.UsingRabbitMq((context, cfg) =>
{
    cfg.Host(
        builder.Configuration["RabbitMq:Host"] ?? "rabbitmq",
        builder.Configuration["RabbitMq:VirtualHost"] ?? "/",
        h =>
        {
            h.Username(builder.Configuration["RabbitMq:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMq:Password"] ?? "guest");
        });
}));

var declarationSeconds = int.TryParse(builder.Configuration["Match:DeclarationSeconds"], out var parsedDeclaration) ? parsedDeclaration : 5;
var turnSeconds = int.TryParse(builder.Configuration["Match:TurnSeconds"], out var parsedTurn) ? parsedTurn : 60;
var botMinDelay = int.TryParse(builder.Configuration["Match:BotMinDelayMs"], out var parsedBotMin) ? parsedBotMin : 800;
var botMaxDelay = int.TryParse(builder.Configuration["Match:BotMaxDelayMs"], out var parsedBotMax) ? parsedBotMax : 1400;

builder.Services.AddSingleton(new MatchRuntimeSettings(declarationSeconds, turnSeconds, botMinDelay, botMaxDelay));
builder.Services.AddScoped<SamMatchApplicationService>();
builder.Services.AddScoped<MatchBroadcaster>();
builder.Services.AddHostedService<MatchAutomationWorker>();
builder.Services.AddHostedService<MatchOutboxPublisherWorker>();

var jwt = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
if (jwt.Secret.Length < 32) throw new InvalidOperationException("Jwt:Secret must contain at least 32 characters.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwt.Issuer,
        ValidAudience = jwt.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
        ClockSkew = TimeSpan.FromSeconds(30)
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var token = context.Request.Query["access_token"];
            if (!string.IsNullOrWhiteSpace(token) &&
                context.HttpContext.Request.Path.StartsWithSegments("/hubs/samloc"))
                context.Token = token;
            return Task.CompletedTask;
        }
    };
});

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapHub<SamLocHub>("/hubs/samloc");
app.MapMatchEndpoints();
app.MapGet("/api/rules/smoke", () =>
{
    var straight = new[] { CardCode.Parse("AS"), CardCode.Parse("2C"), CardCode.Parse("3D") };
    return Results.Ok(new
    {
        service = "SamLocService",
        detected = CombinationDetector.Detect(straight).ToString(),
        straightStrength = StraightRules.TryGetStrength(straight, out var strength) ? strength : -1
    });
});

await using (var scope = app.Services.CreateAsyncScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SamLocDbContext>>();
    await using var db = await factory.CreateDbContextAsync();
    await db.Database.MigrateAsync();
}

app.Run();
