using Application.Common.Interfaces.Repositories;
using Domain.Categories;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class CategoryRepository(ApplicationDbContext context) : ICategoryRepository
{
    public async Task<Option<Category>> GetByNameAsync(string name, CancellationToken ct)
        => await context.Categories
            .AsNoTracking()
            .SingleOrDefaultAsync(category => category.Name == name, ct);

    public async Task<Category> AddAsync(Category category, CancellationToken ct)
    {
        await context.Categories.AddAsync(category, ct);
        await context.SaveChangesAsync(ct);

        return category;
    }
}
