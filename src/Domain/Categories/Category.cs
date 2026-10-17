namespace Domain.Categories;

public class Category
{
    public CategoryId Id { get; private set; }
    public string Name { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public ICollection<ProductCategory> Products { get; private set; } = [];

    private Category(CategoryId id, string name, DateTime createdAt)
    {
        Id = id;
        Name = name;
        CreatedAt = createdAt;
    }

    public static Category New(CategoryId id, string name)
        => new(id, name, DateTime.UtcNow);
}