using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using TienLenService.Api;
using TienLenService.Api.Hubs;
using TienLenService.Application.Matches;
using TienLenService.Domain.Cards;
using TienLenService.Domain.Rules;
using TienLenService.Infrastructure.Matches;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks();
builder.Services.AddSignalR();
builder.Services.AddAuthorization();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().SetIsOriginAllowed(_ => true).AllowCredentials()));
builder.Services.AddSingleton<IMatchStore, InMemoryMatchStore>();
builder.Services.AddSingleton<TienLenMatchApplicationService>();

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
            if (!string.IsNullOrWhiteSpace(token) && context.HttpContext.Request.Path.StartsWithSegments("/hubs/tienlen")) context.Token = token;
            return Task.CompletedTask;
        }
    };
});

var app = builder.Build();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapHub<TienLenHub>("/hubs/tienlen");
app.MapMatchEndpoints();
app.MapGet("/api/rules/smoke", () =>
{
    var pair = new[] { new Card(Rank.Seven, Suit.Spades), new Card(Rank.Seven, Suit.Hearts) };
    return Results.Ok(new { service = "TienLenService", detected = CombinationDetector.Detect(pair).ToString() });
});
app.Run();
