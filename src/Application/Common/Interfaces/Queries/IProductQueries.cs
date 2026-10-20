using Domain.Products;
using LanguageExt;

namespace Application.Common.Interfaces.Queries;

public interface IProductQueries
{
    // Разом із категоріями, без зображень; сортування за CreatedAt desc.
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct);

    // Деталі: категорії та зображення.
    Task<Option<Product>> GetByIdAsync(ProductId id, CancellationToken ct);
}
