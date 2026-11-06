using Application.Common.Interfaces.Repositories;
using Domain.Products;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class ProductRepository(ApplicationDbContext context) : IProductRepository
{
    // З трекінгом і категоріями: UpdateProduct міняє товар і пересинхронізує зв'язки,
    // а SaveChanges сам знаходить додані й прибрані ProductCategory.
    public async Task<Option<Product>> GetByIdAsync(ProductId id, CancellationToken ct)
        => await context.Products
            .Include(product => product.Categories)
            .SingleOrDefaultAsync(product => product.Id == id, ct);

    public async Task<Option<Product>> GetByTitleAsync(string title, CancellationToken ct)
        => await context.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(product => product.Title == title, ct);

    public async Task<Product> AddAsync(Product product, CancellationToken ct)
    {
        await context.Products.AddAsync(product, ct);
        await context.SaveChangesAsync(ct);

        return product;
    }

    public async Task<Product> UpdateAsync(Product product, CancellationToken ct)
    {
        await context.SaveChangesAsync(ct);

        return product;
    }

    public async Task<Product> DeleteAsync(Product product, CancellationToken ct)
    {
        context.Products.Remove(product);
        await context.SaveChangesAsync(ct);

        return product;
    }
}
