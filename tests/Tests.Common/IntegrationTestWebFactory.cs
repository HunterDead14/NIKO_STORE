using Application.Common.Interfaces.Services;
using Infrastructure.Persistence;
using Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;
using Tests.Common.Services;
using Xunit;

namespace Tests.Common;

public class IntegrationTestWebFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("niko-store-test-db")
        .WithUsername("niko-store-test-user")
        .WithPassword("niko-store-test-password")
        .Build();

    public InMemoryNotificationService NotificationService { get; } = new();

    public string StorageRootPath { get; } = Path.Combine(
        Path.GetTempPath(),
        "niko-store-tests",
        Guid.NewGuid().ToString("N"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
            {
                RegisterDatabase(services);
                RegisterTestDoubles(services);
            })
            .ConfigureAppConfiguration((_, config) =>
            {
                config
                    .AddJsonFile("appsettings.Test.json")
                    .AddEnvironmentVariables();
            });
    }

    private void RegisterDatabase(IServiceCollection services)
    {
        services.RemoveServiceByType(typeof(NpgsqlDataSource));
        services.RemoveServiceByType(typeof(DbContextOptions<ApplicationDbContext>));
        services.RemoveServiceByType(typeof(IDbContextOptionsConfiguration<ApplicationDbContext>));

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(_dbContainer.GetConnectionString());
        dataSourceBuilder.EnableDynamicJson();
        var dataSource = dataSourceBuilder.Build();

        services.AddDbContext<ApplicationDbContext>(options => options
            .UseNpgsql(
                dataSource,
                builder => builder.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
            .UseSnakeCaseNamingConvention()
            .ConfigureWarnings(warnings =>
            {
                warnings.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning);
                warnings.Ignore(RelationalEventId.PendingModelChangesWarning);
            }));
    }

    private void RegisterTestDoubles(IServiceCollection services)
    {
        services.RemoveServiceByType(typeof(INotificationService));
        services.AddSingleton<INotificationService>(NotificationService);

        services.RemoveServiceByType(typeof(IFileStorage));
        services.AddSingleton<IFileStorage>(new LocalFileStorage(StorageRootPath));
    }

    public Task InitializeAsync()
    {
        Directory.CreateDirectory(StorageRootPath);
        return _dbContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _dbContainer.DisposeAsync();

        if (Directory.Exists(StorageRootPath))
        {
            Directory.Delete(StorageRootPath, recursive: true);
        }
    }
}
