using Domain.Categories;
using LanguageExt;

namespace Application.Common.Interfaces.Repositories;

public interface ICategoryRepository
{
    Task<Option<Category>> GetByNameAsync(string name, CancellationToken ct);

    Task<Category> AddAsync(Category category, CancellationToken ct);
}
