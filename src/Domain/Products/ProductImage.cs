namespace Domain.Products;

public class ProductImage
{
    public ProductImageId Id { get; private set; }
    public ProductId ProductId { get; private set; }
    
    public string OriginalName { get; private set; }
    public string StoredExtension { get; private set; }
    public string ContentType { get; private set; }
    public long SizeBytes { get; private set; }
    public int SortOrder { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    public Product? Product  { get; private set; }
    
    private ProductImage(
        ProductImageId id,
        ProductId productId,
        string originalName,
        string storedExtension,
        string contentType,
        long sizeBytes,
        int sortOrder,
        DateTime createdAt)
    {
        Id = id;
        ProductId = productId;
        OriginalName = originalName;
        StoredExtension = storedExtension;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        SortOrder = sortOrder;
        CreatedAt = createdAt;
    }    
    
    public static ProductImage New(
        ProductImageId id,
        ProductId productId,
        string originalName,
        string storedExtension,
        string contentType,
        long sizeBytes,
        int sortOrder)
        => new(id, productId, originalName, storedExtension, contentType, sizeBytes, sortOrder, DateTime.UtcNow);
}
}