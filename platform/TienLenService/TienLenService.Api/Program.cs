using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TienLenService.Api;
using TienLenService.Api.Hubs;
using TienLenService.Api.Integration;
using TienLenService.Application.Matches;
using TienLenService.Domain.Cards;
using TienLenService.Domain.Rules;
using TienLenService.Infrastructure;
using TienLenService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks();
builder.Services.AddSignalR(options => options.EnableDetailedErrors = builder.Environment.IsDevelopment());
builder.Services.AddAuthorization();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().SetIsOriginAllowed(_ => true).AllowCredentials()));

builder.Services.AddMassTransit(x => x.UsingRabbitMq((context, cfg) =>
{
    cfg.Host(builder.Configuration["RabbitMq:Host"] ?? "rabbitmq", builder.Configuration["RabbitMq:VirtualHost"] ?? "/", h =>
    {
        h.Username(builder.Configuration["RabbitMq:Username"] ?? "guest");
        h.Password(builder.Configuration["RabbitMq:Password"] ?? "guest");
    });
}));

var turnSeconds = int.TryParse(builder.Configuration["Match:TurnSeconds"], out var parsedTurn) ? parsedTurn : 60;
var botMinDelay = int.TryParse(builder.Configuration["Match:BotMinDelayMs"], out var parsedBotMin) ? parsedBotMin : 800;
var botMaxDelay = int.TryParse(builder.Configuration["Match:BotMaxDelayMs"], out var parsedBotMax) ? parsedBotMax : 1400;
builder.Services.AddSingleton(new MatchRuntimeSettings(turnSeconds, botMinDelay, botMaxDelay));
builder.Services.AddScoped<TienLenMatchApplicationService>();
builder.Services.AddScoped<MatchBroadcaster>();
builder.Services.AddHostedService<MatchAutomationWorker>();
builder.Services.AddHostedService<MatchOutboxPublisherWorker>();

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
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var token = context.Request.Query["access_token"];
            if (!string.IsNullOrWhiteSpace(token) && context.HttpContext.Request.Path.StartsWithSegments("/hubs/tienlen")) context.Token = token;
            return Task.CompletedTask;
        }
    };
});

var app = builder.Build();
app.UseCors(); app.UseAuthentication(); app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapHub<TienLenHub>("/hubs/tienlen");
app.MapMatchEndpoints();
app.MapGet("/api/rules/smoke", () =>
{
    var pair = new[] { new Card(Rank.Seven, Suit.Spades), new Card(Rank.Seven, Suit.Hearts) };
    return Results.Ok(new { service = "TienLenService", detected = CombinationDetector.Detect(pair).ToString() });
});
await using (var scope = app.Services.CreateAsyncScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TienLenDbContext>>();
    await using var db = await factory.CreateDbContextAsync();
    await db.Database.MigrateAsync();
}
app.Run();
