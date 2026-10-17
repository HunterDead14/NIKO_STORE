using Domain.Categories;

namespace Domain.Products;

public class Product
{
    public ProductId Id { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public ProductSpecs Specs { get; private set; } = ProductSpecs.Empty();

    public ICollection<ProductCategory> Categories { get; private set; } = [];
    public ICollection<ProductImage> Images { get; private set; } = [];

    private Product(ProductId id, string title, string description, DateTime createdAt)
    {
        Id = id;
        Title = title;
        Description = description;
        CreatedAt = createdAt;
    }

    public static Product New(ProductId id, string title, string description, ProductSpecs specs)
        => new(id, title, description, DateTime.UtcNow) { Specs = specs };

    public void UpdateDetails(string title, string description, ProductSpecs specs)
    {
        Title = title;
        Description = description;
        Specs = specs;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ReplaceCategories(IEnumerable<CategoryId> categoryIds)
    {
        var target = categoryIds.Distinct().ToHashSet();

        foreach (var link in Categories.Where(x => !target.Contains(x.CategoryId)).ToList())
        {
            Categories.Remove(link);
        }

        var existing = Categories.Select(x => x.CategoryId).ToHashSet();

        foreach (var categoryId in target.Where(x => !existing.Contains(x)))
        {
            Categories.Add(ProductCategory.New(categoryId, Id));
        }
    }
}