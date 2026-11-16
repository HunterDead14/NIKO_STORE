using Domain.Categories;
using LanguageExt;

namespace Application.Common.Interfaces.Repositories;

public interface ICategoryRepository
{
    // Із трекінгом: команда змінює знайдену сутність і зберігає її через UpdateAsync.
    Task<Option<Category>> GetByIdAsync(CategoryId id, CancellationToken ct);

    Task<Option<Category>> GetByNameAsync(string name, CancellationToken ct);

    Task<Category> AddAsync(Category category, CancellationToken ct);

    Task<Category> UpdateAsync(Category category, CancellationToken ct);
}
