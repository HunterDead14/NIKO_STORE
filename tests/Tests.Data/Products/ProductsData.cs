using Domain.Categories;
using Domain.Products;

namespace Tests.Data.Products;

public static class ProductsData
{
    public static ProductSpecs FenderSpecs()
        => new("Fender", "Stratocaster Player", "Sunburst", "New", 2022);

    public static Product MainProduct(params Category[] categories)
        => New(UniqueTitle("Fender Stratocaster"), "Electric guitar, alder body, 22 frets.", FenderSpecs(), categories);

    public static Product SecondProduct(params Category[] categories)
        => New(UniqueTitle("Boss DS-1"), "Distortion pedal.", ProductSpecs.Empty(), categories);

    public static Product FirstTestProduct(params Category[] categories)
        => New(
            UniqueTitle("Fender Stratocaster"),
            "Electric guitar with alder body",
            new ProductSpecs("Fender", "Stratocaster", "Sunburst", "New", 2020),
            categories);

    public static Product SecondTestProduct(params Category[] categories)
        => New(
            UniqueTitle("Gibson Les Paul"),
            "Electric guitar with mahogany body",
            new ProductSpecs("Gibson", "Les Paul", "Cherry", "Used", 2018),
            categories);

    public static Product ThirdTestProduct(params Category[] categories)
        => New(
            UniqueTitle("Yamaha Acoustic"),
            "Acoustic guitar for beginners",
            new ProductSpecs("Yamaha", "FG800", "Natural", "New", 2022),
            categories);

    public static Product FourthTestProduct(params Category[] categories)
        => New(
            UniqueTitle("Ibanez Bass"),
            "Bass guitar with active pickups",
            new ProductSpecs("Ibanez", "SR500", "Black", "Used", 2019),
            categories);

    public static Product New(string title, string description, ProductSpecs specs, params Category[] categories)
    {
        var product = Product.New(ProductId.New(), title, description, specs);
        product.ReplaceCategories(categories.Select(category => category.Id));

        return product;
    }

    public static string UniqueTitle(string prefix)
        => $"{prefix} {Guid.NewGuid():N}"[..Math.Min(prefix.Length + 9, 255)];
}
