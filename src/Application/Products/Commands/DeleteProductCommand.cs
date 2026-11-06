using Application.Common.Interfaces.Repositories;
using Application.Common.Interfaces.Services;
using Application.ProductImages;
using Application.Products.Exceptions;
using Domain.Products;
using LanguageExt;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Products.Commands;

public sealed record DeleteProductCommand(Guid ProductId) : IRequest<Either<ProductException, Product>>;

public sealed class DeleteProductCommandHandler(
    IProductRepository productRepository,
    IFileStorage fileStorage,
    ILogger<DeleteProductCommandHandler> logger)
    : IRequestHandler<DeleteProductCommand, Either<ProductException, Product>>
{
    public Task<Either<ProductException, Product>> Handle(
        DeleteProductCommand request,
        CancellationToken cancellationToken)
    {
        var productId = new ProductId(request.ProductId);

        return productRepository
            .GetByIdAsync(productId, cancellationToken)
            .MatchAsync(
                Some: product => DeleteEntityAsync(product, cancellationToken),
                None: () => new ProductNotFoundException(productId));
    }

    private async Task<Either<ProductException, Product>> DeleteEntityAsync(
        Product product,
        CancellationToken cancellationToken)
    {
        try
        {
            // Зв'язки з категоріями й записи зображень БД видаляє каскадом.
            await productRepository.DeleteAsync(product, cancellationToken);
        }
        catch (Exception exception)
        {
            return new ProductUnhandledException(product.Id, exception);
        }

        await CleanupStorageAsync(product.Id, cancellationToken);

        return product;
    }

    // Best-effort (розд. 7.5 ТЗ): джерело істини — БД, тож невдача з файлами
    // лише пишеться в лог і не скасовує 204.
    private async Task CleanupStorageAsync(ProductId productId, CancellationToken cancellationToken)
    {
        try
        {
            await fileStorage.DeleteDirectoryAsync(ProductImagePaths.ProductDirectory(productId), cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to clean up image folder of deleted product {ProductId}", productId);
        }
    }
}
