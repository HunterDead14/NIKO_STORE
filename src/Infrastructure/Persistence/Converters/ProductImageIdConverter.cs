using Domain.Products;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Persistence.Converters;

public sealed class ProductImageIdConverter : ValueConverter<ProductImageId, Guid>
{
    public ProductImageIdConverter()
        : base(id => id.Value, value => new ProductImageId(value))
    {
    }
}
