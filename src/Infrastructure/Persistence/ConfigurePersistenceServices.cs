using Application.Common.Interfaces;
using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
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
        services.AddScoped<CategoryRepository>();
        services.AddScoped<ICategoryRepository>(provider => provider.GetRequiredService<CategoryRepository>());
        services.AddScoped<ICategoryQueries>(provider => provider.GetRequiredService<CategoryRepository>());

        services.AddScoped<ProductRepository>();
        services.AddScoped<IProductRepository>(provider => provider.GetRequiredService<ProductRepository>());
        services.AddScoped<IProductQueries>(provider => provider.GetRequiredService<ProductRepository>());

        services.AddScoped<ProductImageRepository>();
        services.AddScoped<IProductImageRepository>(provider => provider.GetRequiredService<ProductImageRepository>());
        services.AddScoped<IProductImageQueries>(provider => provider.GetRequiredService<ProductImageRepository>());
    }
}
