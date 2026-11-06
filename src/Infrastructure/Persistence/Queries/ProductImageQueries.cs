using Application.Common.Interfaces.Queries;
using Domain.Products;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Queries;

public class ProductImageQueries(ApplicationDbContext context) : IProductImageQueries
{
    public async Task<IReadOnlyList<ProductImage>> GetByProductIdAsync(ProductId productId, CancellationToken ct)
        => await context.ProductImages
            .AsNoTracking()
            .Where(image => image.ProductId == productId)
            .OrderBy(image => image.SortOrder)
            .ThenBy(image => image.CreatedAt)
            .ToListAsync(ct);

    public async Task<Option<ProductImage>> GetByIdAsync(
        ProductId productId,
        ProductImageId imageId,
        CancellationToken ct)
        => await context.ProductImages
            .AsNoTracking()
            .SingleOrDefaultAsync(image => image.ProductId == productId && image.Id == imageId, ct);
}
