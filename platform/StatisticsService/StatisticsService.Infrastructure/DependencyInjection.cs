using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StatisticsService.Application;
using StatisticsService.Infrastructure.Persistence;

namespace StatisticsService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var cs = configuration.GetConnectionString("RoyalDatabase") ?? throw new InvalidOperationException("ConnectionStrings:RoyalDatabase is required.");
        var provider = configuration["Database:Provider"]?.Trim().ToLowerInvariant() ?? "postgres";
        services.AddDbContextFactory<StatisticsDbContext>(options =>
        {
            if (provider is "sqlserver" or "mssql") options.UseSqlServer(cs, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory_statistics"));
            else if (provider is "postgres" or "postgresql") options.UseNpgsql(cs, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory_statistics"));
            else throw new NotSupportedException($"Unsupported database provider '{provider}'.");
        });
        services.AddScoped<IStatisticsRepository, StatisticsRepository>();
        return services;
    }
}
