using Application.Common.Interfaces.Services;
using Infrastructure.Persistence;
using Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Testcontainers.PostgreSql;
using Tests.Common.Services;

namespace Tests.Common;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:18-alpine")
        .Build();

    private readonly string _storageRoot = Path.Combine(
        Path.GetTempPath(),
        "niko-store-tests",
        Guid.NewGuid().ToString("N"));

    public InMemoryNotificationService NotificationService { get; } = new();

    public string StorageRoot => _storageRoot;

    public async Task StartAsync()
    {
        await _database.StartAsync();
        Directory.CreateDirectory(_storageRoot);

        using var client = CreateClient();
    }

    public async Task StopAsync()
    {
        await DisposeAsync();
        await _database.DisposeAsync();

        if (Directory.Exists(_storageRoot))
        {
            Directory.Delete(_storageRoot, recursive: true);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<NpgsqlDataSource>();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IFileStorage>();
            services.RemoveAll<INotificationService>();

            var dataSourceBuilder = new NpgsqlDataSourceBuilder(_database.GetConnectionString());
            dataSourceBuilder.EnableDynamicJson();
            var dataSource = dataSourceBuilder.Build();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(dataSource)
                .UseSnakeCaseNamingConvention()
                .ConfigureWarnings(warnings => warnings
                    .Ignore(CoreEventId.ManyServiceProvidersCreatedWarning)
                    .Ignore(RelationalEventId.PendingModelChangesWarning))
                .Options;

            services.AddSingleton(dataSource);
            services.AddSingleton(options);
            services.AddSingleton<IFileStorage>(new LocalFileStorage(_storageRoot));
            services.AddSingleton<INotificationService>(NotificationService);
        });
    }
}
