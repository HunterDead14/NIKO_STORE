using Domain.Products;
using LanguageExt;

namespace Application.Common.Interfaces.Repositories;

public interface IProductImageRepository
{
    // Лише за парою {productId, imageId}: чуже зображення не знаходиться (розд. 7.5 ТЗ).
    Task<Option<ProductImage>> GetByIdAsync(ProductId productId, ProductImageId imageId, CancellationToken ct);

    // max(SortOrder) + 1 для товару; перше зображення отримує 0.
    Task<int> GetNextSortOrderAsync(ProductId productId, CancellationToken ct);

    Task<ProductImage> AddAsync(ProductImage image, CancellationToken ct);
    Task<ProductImage> DeleteAsync(ProductImage image, CancellationToken ct);
}
