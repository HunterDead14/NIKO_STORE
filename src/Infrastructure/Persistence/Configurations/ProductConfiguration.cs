using Domain.Products;
using Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(product => product.Id);

        builder.Property(product => product.Id)
            .HasConversion(new ProductIdConverter());

        builder.Property(product => product.Title)
            .HasMaxLength(255)
            .IsRequired();

        builder.HasIndex(product => product.Title)
            .IsUnique();

        builder.Property(product => product.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(product => product.CreatedAt)
            .HasConversion(new DateTimeUtcConverter())
            .HasDefaultValueSql("timezone('utc', now())");

        builder.Property(product => product.UpdatedAt)
            .HasConversion(new DateTimeUtcConverter());

        builder.OwnsOne(product => product.Specs, specs =>
        {
            specs.ToJson("specs");
        });
    }
}
