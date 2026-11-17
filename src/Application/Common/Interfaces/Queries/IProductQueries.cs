using Domain.Products;
using LanguageExt;

namespace Application.Common.Interfaces.Queries;

public interface IProductQueries
{
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct);

    Task<Option<Product>> GetByIdAsync(ProductId id, CancellationToken ct);

    Task<bool> ExistsAsync(ProductId id, CancellationToken ct);
}
