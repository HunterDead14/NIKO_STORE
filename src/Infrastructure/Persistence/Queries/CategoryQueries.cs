using Application.Common.Interfaces.Queries;
using Domain.Categories;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Queries;

public class CategoryQueries(ApplicationDbContext context) : ICategoryQueries
{
    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct)
        => await context.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .ToListAsync(ct);

    public async Task<Option<Category>> GetByIdAsync(CategoryId id, CancellationToken ct)
        => await context.Categories
            .AsNoTracking()
            .SingleOrDefaultAsync(category => category.Id == id, ct);

    public async Task<IReadOnlyList<CategoryId>> GetExistingIdsAsync(
        IReadOnlyCollection<CategoryId> ids,
        CancellationToken ct)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        return await context.Categories
            .AsNoTracking()
            .Where(category => ids.Contains(category.Id))
            .Select(category => category.Id)
            .ToListAsync(ct);
    }
}
