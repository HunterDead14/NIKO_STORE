using Domain.Categories;
using Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.ToTable("product_categories");

        // Складений PK: сурогатного ключа в link-таблиці немає (розд. 4.4).
        builder.HasKey(link => new { link.CategoryId, link.ProductId });

        builder.Property(link => link.CategoryId)
            .HasConversion(new CategoryIdConverter());

        builder.Property(link => link.ProductId)
            .HasConversion(new ProductIdConverter());

        builder.HasOne(link => link.Category)
            .WithMany(category => category.Products)
            .HasForeignKey(link => link.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(link => link.Product)
            .WithMany(product => product.Categories)
            .HasForeignKey(link => link.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
