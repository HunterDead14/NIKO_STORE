using Domain.Products;

namespace Application.Common.Interfaces.Services;

public interface INotificationService
{
    Task SendProductCreatedAsync(Product product, CancellationToken ct);
}
