using Application.Common.Interfaces.Repositories;
using Domain.Products;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class ProductImageRepository(ApplicationDbContext context) : IProductImageRepository
{
    public async Task<Option<ProductImage>> GetByIdAsync(
        ProductId productId,
        ProductImageId imageId,
        CancellationToken ct)
        => await context.ProductImages
            .SingleOrDefaultAsync(image => image.ProductId == productId && image.Id == imageId, ct);

    public async Task<int> GetNextSortOrderAsync(ProductId productId, CancellationToken ct)
    {
        // MaxAsync по int? повертає null, коли зображень ще немає → перше отримує 0.
        var maxSortOrder = await context.ProductImages
            .Where(image => image.ProductId == productId)
            .MaxAsync(image => (int?)image.SortOrder, ct);

        return (maxSortOrder ?? -1) + 1;
    }

    public async Task<ProductImage> AddAsync(ProductImage image, CancellationToken ct)
    {
        await context.ProductImages.AddAsync(image, ct);
        await context.SaveChangesAsync(ct);

        return image;
    }

    public async Task<ProductImage> DeleteAsync(ProductImage image, CancellationToken ct)
    {
        context.ProductImages.Remove(image);
        await context.SaveChangesAsync(ct);

        return image;
    }
}
