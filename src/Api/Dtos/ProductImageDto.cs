using Domain.Products;

namespace Api.Dtos;

public sealed record ProductImageDto(
    Guid Id,
    string OriginalName,
    string ContentType,
    long SizeBytes,
    int SortOrder,
    string ImageUrl)
{
    public static ProductImageDto FromDomainModel(ProductImage image)
        => new(
            image.Id.Value,
            image.OriginalName,
            image.ContentType,
            image.SizeBytes,
            image.SortOrder,
            BuildImageUrl(image.ProductId, image.Id));

    // Відносний шлях, що збігається з маршрутом віддачі байтів (розд. 7.3 ТЗ).
    // Повний URL у БД не зберігається — обчислюється на льоту.
    public static string BuildImageUrl(ProductId productId, ProductImageId imageId)
        => $"/products/{productId.Value}/images/{imageId.Value}";
}
