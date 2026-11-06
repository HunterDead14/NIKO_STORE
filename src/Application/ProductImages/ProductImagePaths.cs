using Domain.Products;

namespace Application.ProductImages;

// Єдине місце, де формуються шляхи у сховищі (розд. 7.1 ТЗ).
// Шлях складається лише з Guid-ів і розширення з білого списку — OriginalName сюди не потрапляє.
public static class ProductImagePaths
{
    public static string ProductDirectory(ProductId productId)
        => $"products/{productId.Value}";

    public static string ImageFile(ProductId productId, ProductImageId imageId, string storedExtension)
        => $"{ProductDirectory(productId)}/{imageId.Value}{storedExtension}";

    public static string ImageFile(ProductImage image)
        => ImageFile(image.ProductId, image.Id, image.StoredExtension);
}
