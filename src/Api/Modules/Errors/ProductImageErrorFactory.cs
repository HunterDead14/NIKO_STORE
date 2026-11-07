using Application.ProductImages.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Errors;

public static class ProductImageErrorFactory
{
    // NotFound і OwnerNotFound дають однаковий 404 — контракт єдиний,
    // різниця лише в title (розд. 8 ТЗ).
    public static ObjectResult ToObjectResult(this ProductImageException error)
    {
        var (statusCode, type) = error switch
        {
            ProductImageNotFoundException => (
                StatusCodes.Status404NotFound,
                "https://tools.ietf.org/html/rfc9110#section-15.5.5"),
            ProductImageOwnerNotFoundException => (
                StatusCodes.Status404NotFound,
                "https://tools.ietf.org/html/rfc9110#section-15.5.5"),
            ProductImageInvalidFileException => (
                StatusCodes.Status400BadRequest,
                "https://tools.ietf.org/html/rfc9110#section-15.5.1"),
            ProductImageUnhandledException => (
                StatusCodes.Status500InternalServerError,
                "https://tools.ietf.org/html/rfc9110#section-15.6.1"),
            _ => throw new NotImplementedException(
                $"Error '{error.GetType().Name}' is not mapped to an HTTP status code."),
        };

        return new ObjectResult(new ProblemDetails
        {
            Type = type,
            Title = error.Message,
            Status = statusCode,
        })
        {
            StatusCode = statusCode,
        };
    }
}
