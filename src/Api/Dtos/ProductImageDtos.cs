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

    public static string BuildImageUrl(ProductId productId, ProductImageId imageId)
        => $"/products/{productId.Value}/images/{imageId.Value}";
}
