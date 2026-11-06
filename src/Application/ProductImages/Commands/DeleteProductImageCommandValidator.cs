using FluentValidation;

namespace Application.ProductImages.Commands;

public sealed class DeleteProductImageCommandValidator : AbstractValidator<DeleteProductImageCommand>
{
    public DeleteProductImageCommandValidator()
    {
        RuleFor(command => command.ProductId)
            .NotEqual(Guid.Empty);

        RuleFor(command => command.ImageId)
            .NotEqual(Guid.Empty);
    }
}
