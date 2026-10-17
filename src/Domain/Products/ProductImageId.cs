namespace Domain.Products;

public record ProductImageId(Guid Value)
{
    public static ProductImageId New() => new(Guid.NewGuid());
    public static ProductImageId Empty() => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}