using Api.Dtos;
using Api.Modules.Errors;
using Application.Common.Interfaces.Queries;
using Application.Products.Commands;
using Application.Products.Exceptions;
using Domain.Products;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("products")]
public sealed class ProductsController(ISender sender, IProductQueries productQueries) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProductListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductListItemDto>>> GetAll(CancellationToken cancellationToken)
    {
        var products = await productQueries.GetAllAsync(cancellationToken);

        return Ok(products.Select(ProductListItemDto.FromDomainModel).ToList());
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<ActionResult<ProductDetailsDto>> GetById(Guid id, CancellationToken cancellationToken)
        => LoadDetailsAsync(new ProductId(id), Ok, cancellationToken);

    [HttpPost]
    [ProducesResponseType(typeof(ProductDetailsDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductDetailsDto>> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateProductCommand(
            request.Title,
            request.Description,
            request.CategoryIds,
            request.Specs?.ToDomainModel());

        var result = await sender.Send(command, cancellationToken);

        return await result.MatchAsync(
            RightAsync: product => LoadDetailsAsync(
                product.Id,
                details => CreatedAtAction(nameof(GetById), new { id = details.Id }, details),
                cancellationToken),
            Left: error => error.ToObjectResult());
    }

    [HttpPut]
    [ProducesResponseType(typeof(ProductDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductDetailsDto>> Update(
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateProductCommand(
            request.Id,
            request.Title,
            request.Description,
            request.CategoryIds,
            request.Specs?.ToDomainModel());

        var result = await sender.Send(command, cancellationToken);

        return await result.MatchAsync(
            RightAsync: product => LoadDetailsAsync(product.Id, Ok, cancellationToken),
            Left: error => error.ToObjectResult());
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteProductCommand(id), cancellationToken);

        return result.Match<IActionResult>(
            _ => NoContent(),
            error => error.ToObjectResult());
    }

    // Відповідь будується з IProductQueries, щоб categories містили назви, а не лише id.
    private async Task<ActionResult<ProductDetailsDto>> LoadDetailsAsync(
        ProductId productId,
        Func<ProductDetailsDto, ActionResult> onFound,
        CancellationToken cancellationToken)
    {
        var product = await productQueries.GetByIdAsync(productId, cancellationToken);

        return product.Match<ActionResult<ProductDetailsDto>>(
            value => onFound(ProductDetailsDto.FromDomainModel(value)),
            () => new ProductNotFoundException(productId).ToObjectResult());
    }
}
