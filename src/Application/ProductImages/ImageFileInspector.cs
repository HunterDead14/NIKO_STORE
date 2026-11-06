using Application.ProductImages.Exceptions;
using LanguageExt;

namespace Application.ProductImages;

public sealed record ImageFileType(string ContentType, string Extension);

public sealed record InspectedImageFile(byte[] Content, ImageFileType Type);

// Перевірка файлу зображення (розд. 6, 7.5 ТЗ): не порожній, ≤ 5 МБ, реальний тип —
// за сигнатурою (magic bytes), без сторонніх бібліотек. Ні Content-Type із запиту,
// ні розширення з імені файлу доказом типу не вважаються.
public static class ImageFileInspector
{
    public const long MaxSizeBytes = 5 * 1024 * 1024;

    public static readonly ImageFileType Jpeg = new("image/jpeg", ".jpg");
    public static readonly ImageFileType Png = new("image/png", ".png");
    public static readonly ImageFileType Webp = new("image/webp", ".webp");

    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] RiffSignature = [0x52, 0x49, 0x46, 0x46];
    private static readonly byte[] WebpSignature = [0x57, 0x45, 0x42, 0x50];
    private const int WebpSignatureOffset = 8;

    // Читає потік у пам'ять, але не більше ніж MaxSizeBytes + 1 байт:
    // цього досить, щоб відрізнити завеликий файл, не тягнучи його повністю.
    public static async Task<Either<ProductImageException, InspectedImageFile>> InspectAsync(
        Stream content,
        CancellationToken cancellationToken)
    {
        var bytes = await ReadLimitedAsync(content, MaxSizeBytes + 1, cancellationToken);

        return Inspect(bytes).Map(type => new InspectedImageFile(bytes, type));
    }

    public static Either<ProductImageException, ImageFileType> Inspect(byte[] content)
        => content.Length switch
        {
            0 => new ProductImageInvalidFileException("file is empty"),
            > (int)MaxSizeBytes => new ProductImageInvalidFileException("file is larger than 5 MB"),
            _ => DetectType(content)
                .ToEither<ProductImageException>(() =>
                    new ProductImageInvalidFileException("only JPEG, PNG and WebP images are allowed")),
        };

    public static Option<ImageFileType> DetectType(byte[] content)
    {
        if (StartsWith(content, PngSignature))
        {
            return Png;
        }

        if (StartsWith(content, JpegSignature))
        {
            return Jpeg;
        }

        if (StartsWith(content, RiffSignature) && HasAt(content, WebpSignatureOffset, WebpSignature))
        {
            return Webp;
        }

        return Option<ImageFileType>.None;
    }

    private static bool StartsWith(byte[] content, byte[] signature)
        => HasAt(content, 0, signature);

    private static bool HasAt(byte[] content, int offset, byte[] signature)
        => content.Length >= offset + signature.Length
           && content.AsSpan(offset, signature.Length).SequenceEqual(signature);

    private static async Task<byte[]> ReadLimitedAsync(Stream content, long limit, CancellationToken cancellationToken)
    {
        using var memory = new MemoryStream();
        var buffer = new byte[81920];

        while (memory.Length < limit)
        {
            var toRead = (int)Math.Min(buffer.Length, limit - memory.Length);
            var read = await content.ReadAsync(buffer.AsMemory(0, toRead), cancellationToken);

            if (read == 0)
            {
                break;
            }

            memory.Write(buffer, 0, read);
        }

        return memory.ToArray();
    }
}
