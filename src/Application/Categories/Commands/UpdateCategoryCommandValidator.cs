using FluentValidation;

namespace Application.Categories.Commands;

public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(command => command.CategoryId)
            .NotEqual(Guid.Empty);

        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(255);
    }
}
