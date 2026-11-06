using Application.Products.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Errors;

public static class ProductErrorFactory
{
    public static ObjectResult ToObjectResult(this ProductException error)
    {
        var (statusCode, type, title) = error switch
        {
            ProductAlreadyExistsException => (
                StatusCodes.Status409Conflict,
                "https://tools.ietf.org/html/rfc9110#section-15.5.10",
                error.Message),
            ProductNotFoundException => (
                StatusCodes.Status404NotFound,
                "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                error.Message),
            ProductCategoriesNotFoundException => (
                StatusCodes.Status404NotFound,
                "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                "One or more categories were not found"),
            ProductUnhandledException => (
                StatusCodes.Status500InternalServerError,
                "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                error.Message),
            _ => throw new NotImplementedException(
                $"Error '{error.GetType().Name}' is not mapped to an HTTP status code."),
        };

        var problemDetails = new ProblemDetails
        {
            Type = type,
            Title = title,
            Status = statusCode,
        };

        // Тіло 404 для невідомих категорій несе їхній перелік (приклад у розд. 5 ТЗ).
        if (error is ProductCategoriesNotFoundException categoriesNotFound)
        {
            problemDetails.Extensions["missingCategoryIds"] = categoriesNotFound.MissingCategoryIds
                .Select(id => id.Value)
                .ToList();
        }

        return new ObjectResult(problemDetails)
        {
            StatusCode = statusCode,
        };
    }
}
