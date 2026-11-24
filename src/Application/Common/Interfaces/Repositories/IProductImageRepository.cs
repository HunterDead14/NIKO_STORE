using Domain.Products;
using LanguageExt;

namespace Application.Common.Interfaces.Repositories;

public interface IProductImageRepository
{
    Task<Option<ProductImage>> GetByIdAsync(ProductId productId, ProductImageId imageId, CancellationToken ct);

    Task<int> GetNextSortOrderAsync(ProductId productId, CancellationToken ct);

    Task<ProductImage> AddAsync(ProductImage image, CancellationToken ct);
    Task<ProductImage> DeleteAsync(ProductImage image, CancellationToken ct);
}
