using LanguageExt;

namespace Application.Common.Interfaces.Services;

// Усі шляхи — відносні до кореня сховища (Storage:RootPath),
// напр. products/{productId}/{imageId}{extension}.
public interface IFileStorage
{
    Task SaveAsync(string relativePath, Stream content, CancellationToken ct);
    Task<Option<Stream>> OpenReadAsync(string relativePath, CancellationToken ct);
    Task DeleteAsync(string relativePath, CancellationToken ct);
    Task DeleteDirectoryAsync(string relativePath, CancellationToken ct);
}
