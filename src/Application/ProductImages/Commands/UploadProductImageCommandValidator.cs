using FluentValidation;

namespace Application.ProductImages.Commands;

// Сам файл тут не перевіряється: порожній, завеликий чи не той тип дає
// ProductImageInvalidFileException → 400 ProblemDetails (розд. 6 ТЗ).
public sealed class UploadProductImageCommandValidator : AbstractValidator<UploadProductImageCommand>
{
    public UploadProductImageCommandValidator()
    {
        RuleFor(command => command.ProductId)
            .NotEqual(Guid.Empty);
    }
}
