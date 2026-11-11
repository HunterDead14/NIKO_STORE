using Domain.Categories;
using Domain.Products;

namespace Tests.Data.Products;

// Фабрики тестових товарів. Назва унікальна (у БД унікальний індекс на title);
// категорії прив'язуються через доменний ReplaceCategories, як у CreateProduct.
public static class ProductsData
{
    public static ProductSpecs FenderSpecs()
        => new("Fender", "Stratocaster Player", "Sunburst", "New", 2022);

    public static Product MainProduct(params Category[] categories)
        => New(UniqueTitle("Fender Stratocaster"), "Electric guitar, alder body, 22 frets.", FenderSpecs(), categories);

    public static Product SecondProduct(params Category[] categories)
        => New(UniqueTitle("Boss DS-1"), "Distortion pedal.", ProductSpecs.Empty(), categories);

    public static Product New(string title, string description, ProductSpecs specs, params Category[] categories)
    {
        var product = Product.New(ProductId.New(), title, description, specs);
        product.ReplaceCategories(categories.Select(category => category.Id));

        return product;
    }

    public static string UniqueTitle(string prefix)
        => $"{prefix} {Guid.NewGuid():N}"[..Math.Min(prefix.Length + 9, 255)];
}
