using Application.Common.Interfaces;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Infrastructure.Persistence;

public static class ConfigurePersistenceServices
{
    public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        // EnableDynamicJson() обов'язковий для jsonb-колонки specs (розд. 10 ТЗ).
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.EnableDynamicJson();

        var dataSource = dataSourceBuilder.Build();
        services.AddSingleton(dataSource);

        services.AddDbContext<ApplicationDbContext>(options =>
            options
                .UseNpgsql(
                    dataSource,
                    npgsql => npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
                .UseSnakeCaseNamingConvention()
                .ConfigureWarnings(warnings => warnings
                    .Ignore(CoreEventId.ManyServiceProvidersCreatedWarning)
                    .Ignore(RelationalEventId.PendingModelChangesWarning)));

        services.AddScoped<ApplicationDbContextInitialiser>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        AddRepositories(services);

        return services;
    }

    private static void AddRepositories(IServiceCollection services)
    {
        // Реалізації репозиторіїв з'являться разом із відповідними командами (Кроки 5–6).
    }
}
