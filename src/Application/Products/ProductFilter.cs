namespace Application.Products;

public sealed record ProductFilter(
    string? Title,
    string? Description,
    string? Brand,
    string? Model,
    string? Color,
    string? Condition,
    int? ManufactureYearFrom,
    int? ManufactureYearTo,
    DateTime? CreatedAtFrom,
    DateTime? CreatedAtTo);
