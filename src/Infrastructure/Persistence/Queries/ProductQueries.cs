using Application.Common.Interfaces.Queries;
using Domain.Products;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Queries;

public class ProductQueries(ApplicationDbContext context) : IProductQueries
{
    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct)
        => await context.Products
            .AsNoTracking()
            .Include(product => product.Categories)
                .ThenInclude(link => link.Category)
            .OrderByDescending(product => product.CreatedAt)
            .ToListAsync(ct);

    public async Task<Option<Product>> GetByIdAsync(ProductId id, CancellationToken ct)
        => await context.Products
            .AsNoTracking()
            .Include(product => product.Categories)
                .ThenInclude(link => link.Category)
            .Include(product => product.Images)
            .AsSplitQuery()
            .SingleOrDefaultAsync(product => product.Id == id, ct);
}
