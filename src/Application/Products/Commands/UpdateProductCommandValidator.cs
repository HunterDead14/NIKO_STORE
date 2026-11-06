using FluentValidation;

namespace Application.Products.Commands;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(command => command.ProductId)
            .NotEqual(Guid.Empty);

        RuleFor(command => command.Title)
            .NotEmpty()
            .Length(3, 255);

        RuleFor(command => command.Description)
            .NotEmpty()
            .Length(3, 500);

        RuleFor(command => command.CategoryIds)
            .NotEmpty();

        RuleForEach(command => command.CategoryIds)
            .NotEqual(Guid.Empty);
    }
}
