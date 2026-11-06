using Api.Dtos;
using Api.Modules.Errors;
using Application.Categories.Commands;
using Application.Categories.Exceptions;
using Application.Common.Interfaces.Queries;
using Domain.Categories;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("categories")]
public sealed class CategoriesController(ISender sender, ICategoryQueries categoryQueries) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryDto>> Create(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateCategoryCommand(request.Name), cancellationToken);

        return result.Match<ActionResult<CategoryDto>>(
            category => CreatedAtAction(
                nameof(GetById),
                new { id = category.Id.Value },
                CategoryDto.FromDomainModel(category)),
            error => error.ToObjectResult());
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var category = await categoryQueries.GetByIdAsync(new CategoryId(id), cancellationToken);

        return category.Match<ActionResult<CategoryDto>>(
            value => Ok(CategoryDto.FromDomainModel(value)),
            () => new CategoryNotFoundException(new CategoryId(id)).ToObjectResult());
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetAll(CancellationToken cancellationToken)
    {
        var categories = await categoryQueries.GetAllAsync(cancellationToken);

        return Ok(categories.Select(CategoryDto.FromDomainModel).ToList());
    }
}
