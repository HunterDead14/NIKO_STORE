using Domain.Categories;
using Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.HasKey(category => category.Id);

        builder.Property(category => category.Id)
            .HasConversion(new CategoryIdConverter());

        builder.Property(category => category.Name)
            .HasMaxLength(255)
            .IsRequired();

        builder.HasIndex(category => category.Name)
            .IsUnique();

        builder.Property(category => category.CreatedAt)
            .HasConversion(new DateTimeUtcConverter())
            .HasDefaultValueSql("timezone('utc', now())");
    }
}
