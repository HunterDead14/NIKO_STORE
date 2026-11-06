using System.Reflection;
using Domain.Categories;
using Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products { get; init; } = null!;
    public DbSet<Category> Categories { get; init; } = null!;
    public DbSet<ProductCategory> ProductCategories { get; init; } = null!;
    public DbSet<ProductImage> ProductImages { get; init; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
