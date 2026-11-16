using Domain.Categories;

namespace Api.Dtos;

public sealed record CategoryDto(Guid Id, string Name, DateTime CreatedAt)
{
    public static CategoryDto FromDomainModel(Category category)
        => new(category.Id.Value, category.Name, category.CreatedAt);
}

public sealed record CreateCategoryRequest(string Name);

// Як і для товарів, id передається в тілі запиту, а не в маршруті.
public sealed record UpdateCategoryRequest(Guid Id, string Name);
