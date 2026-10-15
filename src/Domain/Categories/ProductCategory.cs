using Domain.Products;

namespace Domain.Categories;

public class ProductCategory
{
    public CategoryId CategoryId { get; private set; }
    public Category? Category { get; private set; }

    public ProductId ProductId { get; private set; }
    public Product? Product { get; private set; }

    private ProductCategory(CategoryId categoryId, ProductId productId)
    {
        CategoryId = categoryId;
        ProductId = productId;
    }

    public static ProductCategory New(CategoryId categoryId, ProductId productId)
        => new(categoryId, productId);
}
