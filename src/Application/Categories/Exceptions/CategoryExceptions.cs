using Domain.Categories;

namespace Application.Categories.Exceptions;

public abstract class CategoryException(CategoryId categoryId, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public CategoryId CategoryId { get; } = categoryId;
}

public class CategoryAlreadyExistsException(CategoryId categoryId, string name)
    : CategoryException(categoryId, $"Category with name '{name}' already exists");

public class CategoryNotFoundException(CategoryId categoryId)
    : CategoryException(categoryId, $"Category not found: {categoryId}");

public class CategoryUnhandledException(CategoryId categoryId, Exception? innerException = null)
    : CategoryException(categoryId, "Unexpected error occurred while processing the category", innerException);
