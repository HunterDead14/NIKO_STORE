using Domain.Categories;

namespace Api.Dtos;

public sealed record CategoryDto(Guid Id, string Name, DateTime CreatedAt)
{
    public static CategoryDto FromDomainModel(Category category)
        => new(category.Id.Value, category.Name, category.CreatedAt);
}

public sealed record CreateCategoryRequest(string Name);
