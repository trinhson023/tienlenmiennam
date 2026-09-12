using LobbyService.Application.Abstractions;
using LobbyService.Infrastructure.Integration;
using LobbyService.Infrastructure.Persistence;
using LobbyService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LobbyService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var cs = configuration.GetConnectionString("RoyalDatabase") ?? throw new InvalidOperationException("ConnectionStrings:RoyalDatabase is required.");
        var provider = configuration["Database:Provider"]?.Trim().ToLowerInvariant() ?? "postgres";
        services.AddDbContext<LobbyDbContext>(options =>
        {
            if (provider is "sqlserver" or "mssql") options.UseSqlServer(cs);
            else if (provider is "postgres" or "postgresql") options.UseNpgsql(cs);
            else throw new NotSupportedException($"Unsupported database provider '{provider}'.");
        });
        services.AddScoped<ILobbyRepository, LobbyRepository>();
        services.AddScoped<LobbySeeder>();
        services.AddHttpClient<IMatchLauncher, TienLenMatchLauncher>(client =>
        {
            client.BaseAddress = new Uri(configuration["TienLenService:BaseUrl"] ?? "http://tienlen-api:8080");
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        return services;
    }
}
