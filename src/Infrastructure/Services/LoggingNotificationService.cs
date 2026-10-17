using Application.Common.Interfaces.Services;
using Domain.Products;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

// Заглушка: справжнього каналу сповіщень на першому етапі немає, тож лише пишемо в лог.
public class LoggingNotificationService : INotificationService
{
    private readonly ILogger<LoggingNotificationService> _logger;

    public LoggingNotificationService(ILogger<LoggingNotificationService> logger)
    {
        _logger = logger;
    }

    public Task SendProductCreatedAsync(Product product, CancellationToken ct)
    {
        _logger.LogInformation(
            "Product created: {ProductId} \"{Title}\"",
            product.Id,
            product.Title);

        return Task.CompletedTask;
    }
}
