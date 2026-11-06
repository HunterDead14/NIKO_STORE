using Application.Categories.Exceptions;
using Application.Common.Interfaces.Repositories;
using Domain.Categories;
using LanguageExt;
using MediatR;

namespace Application.Categories.Commands;

public sealed record CreateCategoryCommand(string Name) : IRequest<Either<CategoryException, Category>>;

public sealed class CreateCategoryCommandHandler(ICategoryRepository repository)
    : IRequestHandler<CreateCategoryCommand, Either<CategoryException, Category>>
{
    public Task<Either<CategoryException, Category>> Handle(
        CreateCategoryCommand request,
        CancellationToken cancellationToken)
        => repository
            .GetByNameAsync(request.Name, cancellationToken)
            .MatchAsync(
                Some: existing => new CategoryAlreadyExistsException(existing.Id, request.Name),
                None: () => CreateEntityAsync(request.Name, cancellationToken));

    private async Task<Either<CategoryException, Category>> CreateEntityAsync(
        string name,
        CancellationToken cancellationToken)
    {
        var category = Category.New(CategoryId.New(), name);

        try
        {
            return await repository.AddAsync(category, cancellationToken);
        }
        catch (Exception exception)
        {
            return new CategoryUnhandledException(category.Id, exception);
        }
    }
}
