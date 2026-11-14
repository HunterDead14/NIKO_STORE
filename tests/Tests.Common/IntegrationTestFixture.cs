using Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Tests.Common.Services;
using Xunit;

namespace Tests.Common;

// xUnit створює один IClassFixture на тестовий клас, тому контейнер PostgreSQL
// піднімається раз на клас, а тести в класі виконуються послідовно.
public sealed class IntegrationTestFixture : IAsyncLifetime
{
    private ApiFactory _factory = null!;
    private IServiceScope _scope = null!;
    private HttpClient _client = null!;

    public HttpClient Client => _client;
    public ApplicationDbContext DbContext { get; private set; } = null!;
    public InMemoryNotificationService NotificationService => _factory.NotificationService;
    public string StorageRoot => _factory.StorageRoot;

    public async Task InitializeAsync()
    {
        _factory = new ApiFactory();
        await _factory.StartAsync();

        _client = _factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        DbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    public async Task DisposeAsync()
    {
        DbContext.Dispose();
        _scope.Dispose();
        _client.Dispose();

        await _factory.StopAsync();
    }
}
