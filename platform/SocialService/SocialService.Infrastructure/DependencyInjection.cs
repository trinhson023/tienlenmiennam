using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SocialService.Application;
using SocialService.Infrastructure.Persistence;

namespace SocialService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("RoyalDatabase")
            ?? throw new InvalidOperationException("ConnectionStrings:RoyalDatabase is required.");
        var provider = configuration["Database:Provider"]?.Trim().ToLowerInvariant() ?? "postgres";

        services.AddDbContextFactory<SocialDbContext>(options =>
        {
            if (provider is "sqlserver" or "mssql")
                options.UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory_social"));
            else if (provider is "postgres" or "postgresql")
                options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory_social"));
            else
                throw new NotSupportedException($"Unsupported database provider '{provider}'.");
        });

        services.AddSingleton<IRecentSocialHistory, PersistentSocialHistory>();
        services.AddHttpClient("lobby", client =>
            client.BaseAddress = new Uri(configuration["LobbyService:BaseUrl"] ?? "http://lobby-api:8080"));
        services.AddSingleton<IRoomAccessGateway>(sp => new LobbyRoomAccessGateway(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient("lobby"),
            configuration["InternalApi:Key"] ?? "royal_game_internal_dev_key_change_me"));
        return services;
    }
}
