using Api.Dtos;
using Api.Modules.Errors;
using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Services;
using Application.ProductImages;
using Application.ProductImages.Commands;
using Application.ProductImages.Exceptions;
using Domain.Products;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Api.Controllers;

[ApiController]
[Route("products/{productId:guid}/images")]
public sealed class ProductImagesController(
    ISender sender,
    IProductQueries productQueries,
    IProductImageQueries productImageQueries,
    IFileStorage fileStorage) : ControllerBase
{
    // Шлях містить imageId, тож файл незмінний — можна кешувати надовго (розд. 7.5 ТЗ).
    private const string ImmutableCacheControl = "public, max-age=31536000, immutable";

    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ProductImageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductImageDto>> Upload(
        Guid productId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        await using var content = file.OpenReadStream();

        var result = await sender.Send(
            new UploadProductImageCommand(productId, file.FileName, content),
            cancellationToken);

        return result.Match<ActionResult<ProductImageDto>>(
            image => CreatedAtAction(
                nameof(GetFile),
                new { productId, imageId = image.Id.Value },
                ProductImageDto.FromDomainModel(image)),
            error => error.ToObjectResult());
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProductImageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ProductImageDto>>> GetAll(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var id = new ProductId(productId);

        return await productQueries.ExistsAsync(id, cancellationToken)
            ? Ok((await productImageQueries.GetByProductIdAsync(id, cancellationToken))
                .Select(ProductImageDto.FromDomainModel)
                .ToList())
            : new ProductImageOwnerNotFoundException(id).ToObjectResult();
    }

    [HttpGet("{imageId:guid}")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFile(Guid productId, Guid imageId, CancellationToken cancellationToken)
    {
        var id = new ProductId(productId);
        var image = await productImageQueries.GetByIdAsync(id, new ProductImageId(imageId), cancellationToken);

        return await image.MatchAsync(
            Some: value => OpenFileAsync(value, cancellationToken),
            None: () => NotFoundResultAsync(id, new ProductImageId(imageId), cancellationToken));
    }

    [HttpDelete("{imageId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid productId, Guid imageId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteProductImageCommand(productId, imageId), cancellationToken);

        return result.Match<IActionResult>(
            _ => NoContent(),
            error => error.ToObjectResult());
    }

    // Запис є, а файлу на диску немає (напр. його прибрали вручну) — для клієнта це теж 404.
    private async Task<IActionResult> OpenFileAsync(ProductImage image, CancellationToken cancellationToken)
    {
        var stream = await fileStorage.OpenReadAsync(ProductImagePaths.ImageFile(image), cancellationToken);

        return stream.Match<IActionResult>(
            content =>
            {
                Response.Headers.CacheControl = ImmutableCacheControl;
                return File(content, image.ContentType, lastModified: null, new EntityTagHeaderValue($"\"{image.Id.Value}\""));
            },
            () => new ProductImageNotFoundException(image.Id).ToObjectResult());
    }

    // Обидва випадки — 404; title підказує, чого саме немає: товару чи зображення.
    private async Task<IActionResult> NotFoundResultAsync(
        ProductId productId,
        ProductImageId imageId,
        CancellationToken cancellationToken)
        => await productQueries.ExistsAsync(productId, cancellationToken)
            ? new ProductImageNotFoundException(imageId).ToObjectResult()
            : new ProductImageOwnerNotFoundException(productId).ToObjectResult();
}
