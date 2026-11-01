using Domain.Categories;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Persistence.Converters;

public sealed class CategoryIdConverter : ValueConverter<CategoryId, Guid>
{
    public CategoryIdConverter()
        : base(id => id.Value, value => new CategoryId(value))
    {
    }
}
