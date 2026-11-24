using Domain.Categories;
using Domain.Products;

namespace Tests.Data.Products;

public static class ProductsTestData
{
    public static Product FirstTestProduct(params Category[] categories) => Create(
        "Fender Stratocaster",
        "Electric guitar with alder body",
        new ProductSpecs("Fender", "Stratocaster", "Sunburst", "New", 2020),
        categories);

    public static Product SecondTestProduct(params Category[] categories) => Create(
        "Gibson Les Paul",
        "Electric guitar with mahogany body",
        new ProductSpecs("Gibson", "Les Paul", "Cherry", "Used", 2018),
        categories);

    public static Product ThirdTestProduct(params Category[] categories) => Create(
        "Yamaha Acoustic",
        "Acoustic guitar for beginners",
        new ProductSpecs("Yamaha", "FG800", "Natural", "New", 2022),
        categories);

    public static Product FourthTestProduct(params Category[] categories) => Create(
        "Ibanez Bass",
        "Bass guitar with active pickups",
        new ProductSpecs("Ibanez", "SR500", "Black", "Used", 2019),
        categories);

    private static Product Create(string title, string description, ProductSpecs specs, Category[] categories)
    {
        var product = Product.New(ProductId.New(), title, description, specs);
        product.ReplaceCategories(categories.Select(category => category.Id));

        return product;
    }
}
