using System.Collections.Concurrent;
using Application.Common.Interfaces.Services;
using Domain.Products;

namespace Tests.Common.Services;

public sealed class InMemoryNotificationService : INotificationService
{
    private readonly ConcurrentQueue<ProductId> _sentProductIds = new();

    public IReadOnlyCollection<ProductId> SentProductIds => _sentProductIds.ToArray();

    public Task SendProductCreatedAsync(Product product, CancellationToken ct)
    {
        _sentProductIds.Enqueue(product.Id);
        return Task.CompletedTask;
    }

    public bool WasSentFor(ProductId productId)
        => _sentProductIds.Contains(productId);

    public void Clear()
        => _sentProductIds.Clear();
}
