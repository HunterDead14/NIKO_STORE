using Application.Common.Interfaces.Queries;
using Application.ProductImages.Exceptions;
using Domain.Products;
using LanguageExt;
using Unit = LanguageExt.Unit;

namespace Application.ProductImages.Commands;

internal static class ProductImageOwnerCheck
{
    // Товар із маршруту має існувати, інакше — ProductImageOwnerNotFoundException → 404.
    public static async Task<Either<ProductImageException, Unit>> EnsureProductExistsAsync(
        this IProductQueries productQueries,
        ProductId productId,
        CancellationToken cancellationToken)
        => await productQueries.ExistsAsync(productId, cancellationToken)
            ? Unit.Default
            : new ProductImageOwnerNotFoundException(productId);
}
