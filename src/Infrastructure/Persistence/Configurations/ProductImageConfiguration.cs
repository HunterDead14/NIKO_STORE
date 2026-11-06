using Domain.Products;
using Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("product_images");

        builder.HasKey(image => image.Id);

        builder.Property(image => image.Id)
            .HasConversion(new ProductImageIdConverter());

        builder.Property(image => image.ProductId)
            .HasConversion(new ProductIdConverter());

        builder.Property(image => image.OriginalName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(image => image.StoredExtension)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(image => image.ContentType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(image => image.SizeBytes)
            .IsRequired();

        builder.Property(image => image.SortOrder)
            .IsRequired();

        builder.Property(image => image.CreatedAt)
            .HasConversion(new DateTimeUtcConverter())
            .HasDefaultValueSql("timezone('utc', now())");

        builder.HasIndex(image => new { image.ProductId, image.SortOrder });

        builder.HasOne(image => image.Product)
            .WithMany(product => product.Images)
            .HasForeignKey(image => image.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
