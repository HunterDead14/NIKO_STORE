using Application.Common.Interfaces.Queries;
using Application.Products.Exceptions;
using Domain.Categories;
using Domain.Products;
using LanguageExt;

namespace Application.Products.Commands;

// Спільна перевірка categoryIds для CreateProduct і UpdateProduct (розд. 6 ТЗ:
// «усі id існують — і в POST, і в PUT»).
internal static class ProductCategoriesCheck
{
    public static async Task<Either<ProductException, IReadOnlyList<CategoryId>>> EnsureCategoriesExistAsync(
        this ICategoryQueries categoryQueries,
        ProductId productId,
        IReadOnlyList<Guid> ids,
        CancellationToken cancellationToken)
    {
        var categoryIds = ids.Distinct().Select(id => new CategoryId(id)).ToList();
        var existingIds = await categoryQueries.GetExistingIdsAsync(categoryIds, cancellationToken);
        var missingIds = categoryIds.Except(existingIds).ToList();

        return missingIds.Count == 0
            ? categoryIds
            : new ProductCategoriesNotFoundException(productId, missingIds);
    }
}
