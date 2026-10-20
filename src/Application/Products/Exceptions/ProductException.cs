using Domain.Categories;
using Domain.Products;

namespace Application.Products.Exceptions;

// Помилки не кидаються, а повертаються як Left в Either<ProductException, T>.
// Якщо товару ще не існує (помилка під час створення), передається ProductId.Empty().
public abstract class ProductException(ProductId productId, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public ProductId ProductId { get; } = productId;
}

public class ProductAlreadyExistsException(ProductId productId)
    : ProductException(productId, $"Product with this title already exists: {productId}");

public class ProductNotFoundException(ProductId productId)
    : ProductException(productId, $"Product not found: {productId}");

public class ProductCategoriesNotFoundException(ProductId productId, IReadOnlyList<CategoryId> missingCategoryIds)
    : ProductException(productId, $"Categories not found: {string.Join(", ", missingCategoryIds)}")
{
    public IReadOnlyList<CategoryId> MissingCategoryIds { get; } = missingCategoryIds;
}

public class ProductUnhandledException(ProductId productId, Exception? innerException = null)
    : ProductException(productId, "Unexpected error occurred while processing the product", innerException);
