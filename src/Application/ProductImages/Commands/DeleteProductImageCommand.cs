using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Common.Interfaces.Services;
using Application.ProductImages.Exceptions;
using Domain.Products;
using LanguageExt;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.ProductImages.Commands;

public sealed record DeleteProductImageCommand(
    Guid ProductId,
    Guid ImageId) : IRequest<Either<ProductImageException, ProductImage>>;

public sealed class DeleteProductImageCommandHandler(
    IProductQueries productQueries,
    IProductImageRepository productImageRepository,
    IFileStorage fileStorage,
    ILogger<DeleteProductImageCommandHandler> logger)
    : IRequestHandler<DeleteProductImageCommand, Either<ProductImageException, ProductImage>>
{
    public Task<Either<ProductImageException, ProductImage>> Handle(
        DeleteProductImageCommand request,
        CancellationToken cancellationToken)
    {
        var productId = new ProductId(request.ProductId);
        var imageId = new ProductImageId(request.ImageId);

        // Пошук лише за парою {productId, imageId}: imageId іншого товару → 404 (розд. 5 ТЗ, #11).
        return productQueries
            .EnsureProductExistsAsync(productId, cancellationToken)
            .BindAsync(_ => productImageRepository
                .GetByIdAsync(productId, imageId, cancellationToken)
                .MatchAsync(
                    Some: image => DeleteEntityAsync(image, cancellationToken),
                    None: () => new ProductImageNotFoundException(imageId)));
    }

    private async Task<Either<ProductImageException, ProductImage>> DeleteEntityAsync(
        ProductImage image,
        CancellationToken cancellationToken)
    {
        try
        {
            await productImageRepository.DeleteAsync(image, cancellationToken);
        }
        catch (Exception exception)
        {
            return new ProductImageUnhandledException(image.Id, exception);
        }

        await DeleteFileAsync(image, cancellationToken);

        return image;
    }

    // Запис уже видалено, тож файл без запису — лише сирота на диску: пишемо попередження.
    private async Task DeleteFileAsync(ProductImage image, CancellationToken cancellationToken)
    {
        try
        {
            await fileStorage.DeleteAsync(ProductImagePaths.ImageFile(image), cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to delete file of image {ImageId}", image.Id);
        }
    }
}
