using Application.Categories.Exceptions;
using Application.Common.Interfaces.Repositories;
using Domain.Categories;
using LanguageExt;
using MediatR;
using Unit = LanguageExt.Unit;

namespace Application.Categories.Commands;

public sealed record UpdateCategoryCommand(Guid CategoryId, string Name)
    : IRequest<Either<CategoryException, Category>>;

public sealed class UpdateCategoryCommandHandler(ICategoryRepository repository)
    : IRequestHandler<UpdateCategoryCommand, Either<CategoryException, Category>>
{
    public Task<Either<CategoryException, Category>> Handle(
        UpdateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var categoryId = new CategoryId(request.CategoryId);

        return repository
            .GetByIdAsync(categoryId, cancellationToken)
            .MatchAsync(
                Some: category => CheckDuplicatesAsync(category, request.Name, cancellationToken)
                    .BindAsync(_ => UpdateEntityAsync(category, request.Name, cancellationToken)),
                None: () => new CategoryNotFoundException(categoryId));
    }

    private async Task<Either<CategoryException, Unit>> CheckDuplicatesAsync(
        Category category,
        string name,
        CancellationToken cancellationToken)
    {
        var duplicate = await repository.GetByNameAsync(name, cancellationToken);

        return duplicate
            .Filter(other => other.Id != category.Id)
            .Match<Either<CategoryException, Unit>>(
                Some: other => new CategoryAlreadyExistsException(other.Id, name),
                None: () => Unit.Default);
    }

    private async Task<Either<CategoryException, Category>> UpdateEntityAsync(
        Category category,
        string name,
        CancellationToken cancellationToken)
    {
        try
        {
            category.Rename(name);

            return await repository.UpdateAsync(category, cancellationToken);
        }
        catch (Exception exception)
        {
            return new CategoryUnhandledException(category.Id, exception);
        }
    }
}
