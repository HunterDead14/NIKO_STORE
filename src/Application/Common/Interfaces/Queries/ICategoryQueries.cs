using Domain.Categories;
using LanguageExt;

namespace Application.Common.Interfaces.Queries;

public interface ICategoryQueries
{
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct);

    Task<Option<Category>> GetByIdAsync(CategoryId id, CancellationToken ct);

    Task<IReadOnlyList<CategoryId>> GetExistingIdsAsync(IReadOnlyCollection<CategoryId> ids, CancellationToken ct);
}
