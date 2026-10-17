namespace Domain.Products;

public sealed record ProductSpecs(
    string? Brand,
    string? Model,
    string? Color,
    string? Condition,
    int? ManufactureYear)
{
    public static ProductSpecs Empty() => new(null, null, null, null, null);
}