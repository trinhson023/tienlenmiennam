using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SocialService.Api;
using SocialService.Api.Hubs;
using SocialService.Application;
using SocialService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<SocialThrottle>();
builder.Services.AddSingleton<IRecentSocialHistory, InMemorySocialHistory>();
builder.Services.AddHttpClient("lobby", client => client.BaseAddress = new Uri(builder.Configuration["LobbyService:BaseUrl"] ?? "http://lobby-api:8080"));
builder.Services.AddSingleton<IRoomAccessGateway>(sp => new LobbyRoomAccessGateway(
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("lobby"),
    builder.Configuration["InternalApi:Key"] ?? "royal_game_internal_dev_key_change_me"));
builder.Services.AddScoped<SocialApplicationService>();
builder.Services.AddSignalR(options => options.EnableDetailedErrors = builder.Environment.IsDevelopment());
builder.Services.AddAuthorization();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().SetIsOriginAllowed(_ => true).AllowCredentials()));

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
            if (!string.IsNullOrWhiteSpace(token) && context.HttpContext.Request.Path.StartsWithSegments("/hubs/social")) context.Token = token;
            return Task.CompletedTask;
        }
    };
});

var app = builder.Build();
app.UseCors(); app.UseAuthentication(); app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapGet("/api/status", () => Results.Ok(new { service = "SocialService", status = "m8-social-phase1" })).RequireAuthorization();
app.MapSocialEndpoints();
app.MapHub<SocialHub>("/hubs/social");
app.Run();
