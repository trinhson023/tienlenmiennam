using MediaService.Application;
using MediaService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MediaService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var cs = configuration.GetConnectionString("RoyalDatabase") ?? throw new InvalidOperationException("ConnectionStrings:RoyalDatabase is required.");
        var provider = configuration["Database:Provider"]?.Trim().ToLowerInvariant() ?? "postgres";
        services.AddDbContextFactory<MediaDbContext>(options =>
        {
            if (provider is "sqlserver" or "mssql") options.UseSqlServer(cs, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory_media"));
            else if (provider is "postgres" or "postgresql") options.UseNpgsql(cs, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory_media"));
            else throw new NotSupportedException($"Unsupported database provider '{provider}'.");
        });
        services.AddSingleton<IMediaRoomStore, PersistentMediaRoomStore>();
        services.AddHttpClient("lobby", c => c.BaseAddress = new Uri(configuration["LobbyService:BaseUrl"] ?? "http://lobby-api:8080"));
        services.AddSingleton<IRoomMediaAccessGateway>(sp => new RoomAccessGateway(sp.GetRequiredService<IHttpClientFactory>().CreateClient("lobby"), configuration["InternalApi:Key"] ?? "royal_game_internal_dev_key_change_me"));
        services.AddHttpClient("youtube");
        services.AddSingleton<IYouTubeSearch>(sp => new YouTubeSearchClient(sp.GetRequiredService<IHttpClientFactory>().CreateClient("youtube"), configuration["YouTube:ApiKey"] ?? string.Empty));
        return services;
    }
}
