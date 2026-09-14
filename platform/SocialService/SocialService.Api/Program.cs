using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SocialService.Api;
using SocialService.Api.Hubs;
using SocialService.Application;
using SocialService.Infrastructure;
using SocialService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<SocialThrottle>();
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
app.MapGet("/api/status", () => Results.Ok(new { service = "SocialService", status = "m8-social-persistent" })).RequireAuthorization();
app.MapSocialEndpoints();
app.MapHub<SocialHub>("/hubs/social");

await using (var scope = app.Services.CreateAsyncScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SocialDbContext>>();
    await using var db = await factory.CreateDbContextAsync();
    await db.Database.MigrateAsync();
}

app.Run();
