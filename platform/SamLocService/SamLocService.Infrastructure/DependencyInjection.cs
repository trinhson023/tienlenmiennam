using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SamLocService.Application.Matches;
using SamLocService.Infrastructure.Matches;
using SamLocService.Infrastructure.Persistence;

namespace SamLocService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var cs = configuration.GetConnectionString("RoyalDatabase")
            ?? throw new InvalidOperationException("ConnectionStrings:RoyalDatabase is required.");
        var provider = configuration["Database:Provider"]?.Trim().ToLowerInvariant() ?? "postgres";

        services.AddDbContextFactory<SamLocDbContext>(options =>
        {
            if (provider is "sqlserver" or "mssql")
                options.UseSqlServer(cs, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory_samloc", "samloc"));
            else if (provider is "postgres" or "postgresql")
                options.UseNpgsql(cs, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory_samloc", "samloc"));
            else
                throw new NotSupportedException($"Unsupported database provider '{provider}'.");
        });

        services.AddSingleton<IMatchStore, PersistentMatchStore>();
        return services;
    }
}
