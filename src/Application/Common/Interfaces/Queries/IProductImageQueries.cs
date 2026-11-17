using Domain.Products;
using LanguageExt;

namespace Application.Common.Interfaces.Queries;

public interface IProductImageQueries
{
    Task<IReadOnlyList<ProductImage>> GetByProductIdAsync(ProductId productId, CancellationToken ct);

    Task<Option<ProductImage>> GetByIdAsync(ProductId productId, ProductImageId imageId, CancellationToken ct);
}
