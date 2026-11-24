using Application.Products.Filters;
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

public sealed record CreateProductDto(
    string Title,
    string Description,
    IReadOnlyList<Guid> CategoryIds,
    ProductSpecsDto? Specs);

public sealed record UpdateProductDto(
    Guid Id,
    string Title,
    string Description,
    IReadOnlyList<Guid> CategoryIds,
    ProductSpecsDto? Specs);

public sealed record ProductFilterDto(
    string? Title,
    string? Description,
    string? Brand,
    string? Model,
    string? Color,
    string? Condition,
    int? ManufactureYearFrom,
    int? ManufactureYearTo,
    DateTime? CreatedAtFrom,
    DateTime? CreatedAtTo)
{
    public ProductFilter ToDomainModel()
        => new(
            Title,
            Description,
            Brand,
            Model,
            Color,
            Condition,
            ManufactureYearFrom,
            ManufactureYearTo,
            ToUtc(CreatedAtFrom),
            ToUtc(CreatedAtTo));

    private static DateTime? ToUtc(DateTime? value)
        => value is null
            ? null
            : value.Value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.Value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc),
            };
}

internal static class ProductDtoMapping
{
    public static IReadOnlyList<CategoryDto> MapCategories(Product product)
        => product.Categories
            .Select(link => CategoryDto.FromDomainModel(link.Category!))
            .OrderBy(category => category.Name)
            .ToList();
}
