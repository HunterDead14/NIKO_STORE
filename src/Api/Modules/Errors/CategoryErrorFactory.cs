using Application.Categories.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Errors;

public static class CategoryErrorFactory
{
    public static ObjectResult ToObjectResult(this CategoryException error)
    {
        var (statusCode, type) = error switch
        {
            CategoryAlreadyExistsException => (
                StatusCodes.Status409Conflict,
                "https://tools.ietf.org/html/rfc9110#section-15.5.10"),
            CategoryNotFoundException => (
                StatusCodes.Status404NotFound,
                "https://tools.ietf.org/html/rfc9110#section-15.5.5"),
            CategoryUnhandledException => (
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
