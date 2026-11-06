using Domain.Categories;
using LanguageExt;

namespace Application.Common.Interfaces.Queries;

public interface ICategoryQueries
{
    // Сортування за Name asc (розд. 5 ТЗ).
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct);

    Task<Option<Category>> GetByIdAsync(CategoryId id, CancellationToken ct);

    // Із переданого набору повертає лише ті id, що існують (перевірка categoryIds у Product-командах).
    Task<IReadOnlyList<CategoryId>> GetExistingIdsAsync(IReadOnlyCollection<CategoryId> ids, CancellationToken ct);
}
