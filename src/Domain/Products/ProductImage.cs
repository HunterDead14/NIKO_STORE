namespace Domain.Products;

public class ProductImage
{
    public ProductImageId Id { get; private set; }
    public string OriginalName { get; private set; }

    public ProductId ProductId { get; private set; }
    public Product? Product { get; private set; }

    private ProductImage(ProductImageId id, ProductId productId, string originalName)
    {
        Id = id;
        ProductId = productId;
        OriginalName = originalName;
    }

    public static ProductImage New(ProductImageId id, ProductId productId, string originalName)
        => new(id, productId, originalName);
}
