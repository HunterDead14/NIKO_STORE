using Domain.Products;
using LanguageExt;

namespace Application.Common.Interfaces.Repositories;

public interface IProductRepository
{
    Task<Option<Product>> GetByIdAsync(ProductId id, CancellationToken ct);
    Task<Option<Product>> GetByTitleAsync(string title, CancellationToken ct);

    Task<Product> AddAsync(Product product, CancellationToken ct);
    Task<Product> UpdateAsync(Product product, CancellationToken ct);
    Task<Product> DeleteAsync(Product product, CancellationToken ct);
}
