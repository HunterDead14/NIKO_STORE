using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Persistence;

public static class ConfigurePersistenceServices
{
    public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options
                .UseNpgsql(
                    connectionString,
                    npgsql => npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
                .UseSnakeCaseNamingConvention());

        services.AddScoped<ApplicationDbContextInitialiser>();

        AddRepositories(services);

        return services;
    }

    private static void AddRepositories(IServiceCollection services)
    {
        // Репозиторії реєструються на Кроці 3 (Infrastructure).
    }
}
