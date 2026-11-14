using Infrastructure.Persistence;
using Tests.Common.Services;

namespace Tests.Common;

public abstract class IntegrationTestBase
{
    protected IntegrationTestBase(IntegrationTestFixture fixture)
    {
        Fixture = fixture;

        // Кожен тест починається з чистого трекінгу й без сповіщень;
        // дані в БД лишаються — фабрики даних гарантують унікальні назви.
        Fixture.DbContext.ChangeTracker.Clear();
        Fixture.NotificationService.Clear();
    }

    protected IntegrationTestFixture Fixture { get; }
    protected HttpClient Client => Fixture.Client;
    protected ApplicationDbContext DbContext => Fixture.DbContext;
}
