using Application.ProductImages.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Errors;

public static class ProductImageErrorFactory
{
    public static ObjectResult ToObjectResult(this ProductImageException error)
    {
        if (error is ProductImageInvalidFileException invalidFile)
        {
            var validationProblem = new ValidationProblemDetails(
                new Dictionary<string, string[]>
                {
                    ["File"] = [invalidFile.Message],
                })
            {
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                Title = invalidFile.Message,
                Status = StatusCodes.Status400BadRequest,
            };

            return new ObjectResult(validationProblem)
            {
                StatusCode = StatusCodes.Status400BadRequest,
            };
        }

        var (statusCode, type) = error switch
        {
            ProductImageNotFoundException => (
                StatusCodes.Status404NotFound,
                "https://tools.ietf.org/html/rfc9110#section-15.5.5"),
            ProductImageOwnerNotFoundException => (
                StatusCodes.Status404NotFound,
                "https://tools.ietf.org/html/rfc9110#section-15.5.5"),
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
