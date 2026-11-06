using FluentValidation;

namespace Application.Products.Commands;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
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
