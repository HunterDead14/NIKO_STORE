using Application.Common.Interfaces;
using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Products.Exceptions;
using Domain.Categories;
using Domain.Products;
using LanguageExt;
using MediatR;
using Unit = LanguageExt.Unit;

namespace Application.Products.Commands;

// PUT повністю замінює товар: title, description, specs і рівно той набір категорій,
// що прийшов у тілі (розд. 5 ТЗ, #6). Уся операція — в одній транзакції.
public sealed record UpdateProductCommand(
    Guid ProductId,
    string Title,
    string Description,
    IReadOnlyList<Guid> CategoryIds,
    ProductSpecs? Specs) : IRequest<Either<ProductException, Product>>;

public sealed class UpdateProductCommandHandler(
    IProductRepository productRepository,
    ICategoryQueries categoryQueries,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateProductCommand, Either<ProductException, Product>>
{
    public async Task<Either<ProductException, Product>> Handle(
        UpdateProductCommand request,
        CancellationToken cancellationToken)
    {
        var productId = new ProductId(request.ProductId);

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await productRepository
                .GetByIdAsync(productId, cancellationToken)
                .MatchAsync(
                    Some: product => UpdateEntityAsync(product, request, cancellationToken),
                    None: () => new ProductNotFoundException(productId));

            // Commit лише на Right; на Left усе, що встигли змінити, відкочується.
            await result.Match(
                Right: _ => transaction.CommitAsync(cancellationToken),
                Left: _ => transaction.RollbackAsync(cancellationToken));

            return result;
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new ProductUnhandledException(productId, exception);
        }
    }

    private Task<Either<ProductException, Product>> UpdateEntityAsync(
        Product product,
        UpdateProductCommand request,
        CancellationToken cancellationToken)
        => EnsureTitleIsFreeAsync(product, request.Title, cancellationToken)
            .BindAsync(_ => categoryQueries.EnsureCategoriesExistAsync(product.Id, request.CategoryIds, cancellationToken))
            .BindAsync(categoryIds => SaveAsync(product, request, categoryIds, cancellationToken));

    // Назва може лишитись тією самою; конфлікт — лише коли її вже має інший товар.
    private async Task<Either<ProductException, Unit>> EnsureTitleIsFreeAsync(
        Product product,
        string title,
        CancellationToken cancellationToken)
        => (await productRepository.GetByTitleAsync(title, cancellationToken))
            .Filter(other => other.Id != product.Id)
            .Match<Either<ProductException, Unit>>(
                Some: _ => new ProductAlreadyExistsException(product.Id),
                None: () => Unit.Default);

    private async Task<Either<ProductException, Product>> SaveAsync(
        Product product,
        UpdateProductCommand request,
        IReadOnlyList<CategoryId> categoryIds,
        CancellationToken cancellationToken)
    {
        product.UpdateDetails(request.Title, request.Description, request.Specs ?? ProductSpecs.Empty());
        product.ReplaceCategories(categoryIds);

        return await productRepository.UpdateAsync(product, cancellationToken);
    }
}
