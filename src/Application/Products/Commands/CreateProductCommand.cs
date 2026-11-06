using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Common.Interfaces.Services;
using Application.Products.Exceptions;
using Domain.Categories;
using Domain.Products;
using LanguageExt;
using MediatR;

namespace Application.Products.Commands;

public sealed record CreateProductCommand(
    string Title,
    string Description,
    IReadOnlyList<Guid> CategoryIds,
    ProductSpecs? Specs) : IRequest<Either<ProductException, Product>>;

public sealed class CreateProductCommandHandler(
    IProductRepository productRepository,
    ICategoryQueries categoryQueries,
    INotificationService notificationService)
    : IRequestHandler<CreateProductCommand, Either<ProductException, Product>>
{
    public Task<Either<ProductException, Product>> Handle(
        CreateProductCommand request,
        CancellationToken cancellationToken)
        => productRepository
            .GetByTitleAsync(request.Title, cancellationToken)
            .MatchAsync(
                Some: existing => new ProductAlreadyExistsException(existing.Id),
                None: () => CheckCategoriesAsync(request.CategoryIds, cancellationToken)
                    .BindAsync(categoryIds => CreateEntityAsync(request, categoryIds, cancellationToken)));

    private async Task<Either<ProductException, IReadOnlyList<CategoryId>>> CheckCategoriesAsync(
        IReadOnlyList<Guid> ids,
        CancellationToken cancellationToken)
    {
        var categoryIds = ids.Distinct().Select(id => new CategoryId(id)).ToList();
        var existingIds = await categoryQueries.GetExistingIdsAsync(categoryIds, cancellationToken);
        var missingIds = categoryIds.Except(existingIds).ToList();

        return missingIds.Count == 0
            ? categoryIds
            : new ProductCategoriesNotFoundException(ProductId.Empty(), missingIds);
    }

    private async Task<Either<ProductException, Product>> CreateEntityAsync(
        CreateProductCommand request,
        IReadOnlyList<CategoryId> categoryIds,
        CancellationToken cancellationToken)
    {
        var product = Product.New(
            ProductId.New(),
            request.Title,
            request.Description,
            request.Specs ?? ProductSpecs.Empty());

        product.ReplaceCategories(categoryIds);

        try
        {
            await productRepository.AddAsync(product, cancellationToken);
            await notificationService.SendProductCreatedAsync(product, cancellationToken);

            return product;
        }
        catch (Exception exception)
        {
            return new ProductUnhandledException(product.Id, exception);
        }
    }
}
