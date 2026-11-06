using Domain.Products;

namespace Api.Dtos;

public sealed record ProductSpecsDto(
    string? Brand,
    string? Model,
    string? Color,
    string? Condition,
    int? ManufactureYear)
{
    public static ProductSpecsDto FromDomainModel(ProductSpecs specs)
        => new(specs.Brand, specs.Model, specs.Color, specs.Condition, specs.ManufactureYear);

    public ProductSpecs ToDomainModel()
        => new(Brand, Model, Color, Condition, ManufactureYear);
}

// Елемент списку: без зображень (розд. 5 ТЗ, «Формат DTO»).
public sealed record ProductListItemDto(
    Guid Id,
    string Title,
    string Description,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    ProductSpecsDto Specs,
    IReadOnlyList<CategoryDto> Categories)
{
    public static ProductListItemDto FromDomainModel(Product product)
        => new(
            product.Id.Value,
            product.Title,
            product.Description,
            product.CreatedAt,
            product.UpdatedAt,
            ProductSpecsDto.FromDomainModel(product.Specs),
            ProductDtoMapping.MapCategories(product));
}

public sealed record ProductDetailsDto(
    Guid Id,
    string Title,
    string Description,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    ProductSpecsDto Specs,
    IReadOnlyList<CategoryDto> Categories,
    IReadOnlyList<ProductImageDto> Images)
{
    public static ProductDetailsDto FromDomainModel(Product product)
        => new(
            product.Id.Value,
            product.Title,
            product.Description,
            product.CreatedAt,
            product.UpdatedAt,
            ProductSpecsDto.FromDomainModel(product.Specs),
            ProductDtoMapping.MapCategories(product),
            product.Images
                .OrderBy(image => image.SortOrder)
                .ThenBy(image => image.CreatedAt)
                .Select(ProductImageDto.FromDomainModel)
                .ToList());
}

public sealed record CreateProductRequest(
    string Title,
    string Description,
    IReadOnlyList<Guid> CategoryIds,
    ProductSpecsDto? Specs);

// Id — у тілі, а не в маршруті: PUT /products (розд. 5 ТЗ, #6).
public sealed record UpdateProductRequest(
    Guid Id,
    string Title,
    string Description,
    IReadOnlyList<Guid> CategoryIds,
    ProductSpecsDto? Specs);

internal static class ProductDtoMapping
{
    // У відповідях — CategoryDto (id, назва), а не елементи link-таблиці.
    // Queries завжди підвантажують Category для кожного зв'язку.
    public static IReadOnlyList<CategoryDto> MapCategories(Product product)
        => product.Categories
            .Select(link => CategoryDto.FromDomainModel(link.Category!))
            .OrderBy(category => category.Name)
            .ToList();
}
