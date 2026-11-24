using Application.Common.Interfaces.Queries;
using Application.Products;
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

    public async Task<IReadOnlyList<Product>> FilterAsync(ProductFilter filter, CancellationToken ct)
    {
        var query = context.Products
            .AsNoTracking()
            .Include(product => product.Categories)
                .ThenInclude(link => link.Category)
            .AsQueryable();

        var title = filter.Title?.Trim();
        if (!string.IsNullOrEmpty(title))
        {
            query = query.Where(product => EF.Functions.ILike(product.Title, $"%{title}%"));
        }

        var description = filter.Description?.Trim();
        if (!string.IsNullOrEmpty(description))
        {
            query = query.Where(product => EF.Functions.ILike(product.Description, $"%{description}%"));
        }

        var brand = filter.Brand?.Trim();
        if (!string.IsNullOrEmpty(brand))
        {
            query = query.Where(product =>
                product.Specs.Brand != null &&
                EF.Functions.ILike(product.Specs.Brand, $"%{brand}%"));
        }

        var model = filter.Model?.Trim();
        if (!string.IsNullOrEmpty(model))
        {
            query = query.Where(product =>
                product.Specs.Model != null &&
                EF.Functions.ILike(product.Specs.Model, $"%{model}%"));
        }

        var color = filter.Color?.Trim();
        if (!string.IsNullOrEmpty(color))
        {
            query = query.Where(product =>
                product.Specs.Color != null &&
                EF.Functions.ILike(product.Specs.Color, $"%{color}%"));
        }

        var condition = filter.Condition?.Trim();
        if (!string.IsNullOrEmpty(condition))
        {
            query = query.Where(product =>
                product.Specs.Condition != null &&
                EF.Functions.ILike(product.Specs.Condition, $"%{condition}%"));
        }

        if (filter.ManufactureYearFrom is { } manufactureYearFrom)
        {
            query = query.Where(product => product.Specs.ManufactureYear >= manufactureYearFrom);
        }

        if (filter.ManufactureYearTo is { } manufactureYearTo)
        {
            query = query.Where(product => product.Specs.ManufactureYear <= manufactureYearTo);
        }

        if (filter.CreatedAtFrom is { } createdAtFrom)
        {
            query = query.Where(product => product.CreatedAt >= createdAtFrom);
        }

        if (filter.CreatedAtTo is { } createdAtTo)
        {
            query = query.Where(product => product.CreatedAt <= createdAtTo);
        }

        return await query
            .OrderByDescending(product => product.CreatedAt)
            .ToListAsync(ct);
    }

    public Task<bool> ExistsAsync(ProductId id, CancellationToken ct)
        => context.Products
            .AsNoTracking()
            .AnyAsync(product => product.Id == id, ct);
}
