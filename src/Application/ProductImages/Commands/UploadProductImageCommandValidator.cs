using FluentValidation;

namespace Application.ProductImages.Commands;

public sealed class UploadProductImageCommandValidator : AbstractValidator<UploadProductImageCommand>
{
    public UploadProductImageCommandValidator()
    {
        RuleFor(command => command.ProductId)
            .NotEqual(Guid.Empty);
    }
}
