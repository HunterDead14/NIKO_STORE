using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Common.Interfaces.Services;
using Application.ProductImages.Exceptions;
using Domain.Products;
using LanguageExt;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.ProductImages.Commands;

// Клієнт передає лише id товару й сам файл; imageId, шлях і розширення генерує сервер (розд. 7.4 ТЗ).
public sealed record UploadProductImageCommand(
    Guid ProductId,
    string FileName,
    Stream Content) : IRequest<Either<ProductImageException, ProductImage>>;

public sealed class UploadProductImageCommandHandler(
    IProductQueries productQueries,
    IProductImageRepository productImageRepository,
    IFileStorage fileStorage,
    ILogger<UploadProductImageCommandHandler> logger)
    : IRequestHandler<UploadProductImageCommand, Either<ProductImageException, ProductImage>>
{
    private const int MaxOriginalNameLength = 255;

    public Task<Either<ProductImageException, ProductImage>> Handle(
        UploadProductImageCommand request,
        CancellationToken cancellationToken)
    {
        var productId = new ProductId(request.ProductId);

        return productQueries
            .EnsureProductExistsAsync(productId, cancellationToken)
            .BindAsync(_ => ImageFileInspector.InspectAsync(request.Content, cancellationToken))
            .BindAsync(file => SaveImageAsync(productId, request.FileName, file, cancellationToken));
    }

    // Порядок за розд. 7.5 ТЗ: спершу запис ProductImage і SaveChanges, потім файл;
    // якщо файл записати не вдалося — прибираємо запис (компенсація).
    private async Task<Either<ProductImageException, ProductImage>> SaveImageAsync(
        ProductId productId,
        string fileName,
        InspectedImageFile file,
        CancellationToken cancellationToken)
    {
        var imageId = ProductImageId.New();
        ProductImage image;

        try
        {
            var sortOrder = await productImageRepository.GetNextSortOrderAsync(productId, cancellationToken);

            image = ProductImage.New(
                imageId,
                productId,
                SanitizeOriginalName(fileName, file.Type.Extension),
                file.Type.Extension,
                file.Type.ContentType,
                file.Content.Length,
                sortOrder);

            await productImageRepository.AddAsync(image, cancellationToken);
        }
        catch (Exception exception)
        {
            return new ProductImageUnhandledException(imageId, exception);
        }

        try
        {
            await using var content = new MemoryStream(file.Content, writable: false);
            await fileStorage.SaveAsync(ProductImagePaths.ImageFile(image), content, cancellationToken);

            return image;
        }
        catch (Exception exception)
        {
            await CompensateAsync(image, cancellationToken);
            return new ProductImageUnhandledException(imageId, exception);
        }
    }

    private async Task CompensateAsync(ProductImage image, CancellationToken cancellationToken)
    {
        try
        {
            await productImageRepository.DeleteAsync(image, CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to remove record of image {ImageId} after storage error", image.Id);
        }
    }

    // OriginalName — лише для показу: без шляху, керуючих і заборонених у імені символів,
    // обрізаний до довжини колонки. На диску ніколи не використовується (розд. 7.5 ТЗ).
    private static string SanitizeOriginalName(string fileName, string extension)
    {
        var name = Path.GetFileName(fileName.Replace('\\', '/'));
        var invalid = Path.GetInvalidFileNameChars();

        var cleaned = new string(name
                .Where(character => !char.IsControl(character) && !invalid.Contains(character))
                .ToArray())
            .Trim();

        var result = string.IsNullOrEmpty(cleaned) ? $"image{extension}" : cleaned;

        return result.Length <= MaxOriginalNameLength ? result : result[..MaxOriginalNameLength];
    }
}
