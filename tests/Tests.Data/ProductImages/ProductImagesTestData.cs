using Domain.Products;

namespace Tests.Data.ProductImages;

public static class ProductImagesTestData
{
    public static ProductImage FirstTestImage(Product product) => ProductImage.New(
        ProductImageId.New(),
        product.Id,
        "guitar.jpg",
        ".jpg",
        "image/jpeg",
        12,
        0);

    public static ProductImage SecondTestImage(Product product) => ProductImage.New(
        ProductImageId.New(),
        product.Id,
        "guitar.png",
        ".png",
        "image/png",
        12,
        1);
}
