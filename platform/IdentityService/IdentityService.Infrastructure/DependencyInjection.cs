using IdentityService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var cs = configuration.GetConnectionString("RoyalDatabase") ?? throw new InvalidOperationException("ConnectionStrings:RoyalDatabase is required.");
        var provider = configuration["Database:Provider"]?.Trim().ToLowerInvariant() ?? "postgres";
        services.AddDbContext<IdentityDbContext>(options =>
        {
            if (provider is "sqlserver" or "mssql") options.UseSqlServer(cs);
            else if (provider is "postgres" or "postgresql") options.UseNpgsql(cs);
            else throw new NotSupportedException($"Unsupported database provider '{provider}'.");
        });
        return services;
    }
}
