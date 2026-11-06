using Domain.Products;

namespace Application.ProductImages.Exceptions;

// Помилки не кидаються, а повертаються як Left в Either<ProductImageException, T>.
// Якщо зображення ще не існує, передається ProductImageId.Empty().
public abstract class ProductImageException(ProductImageId productImageId, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public ProductImageId ProductImageId { get; } = productImageId;
}

public class ProductImageNotFoundException(ProductImageId productImageId)
    : ProductImageException(productImageId, $"Product image not found: {productImageId}");

public class ProductImageOwnerNotFoundException(ProductId productId)
    : ProductImageException(ProductImageId.Empty(), $"Product not found: {productId}")
{
    public ProductId ProductId { get; } = productId;
}

public class ProductImageInvalidFileException(string reason)
    : ProductImageException(ProductImageId.Empty(), $"Invalid image file: {reason}");

public class ProductImageUnhandledException(ProductImageId productImageId, Exception? innerException = null)
    : ProductImageException(productImageId, "Unexpected error occurred while processing the product image", innerException);
