using LanguageExt;

namespace Application.Common.Interfaces.Services;

public interface IFileStorage
{
    Task SaveAsync(string relativePath, Stream content, CancellationToken ct);
    Task<Option<Stream>> OpenReadAsync(string relativePath, CancellationToken ct);
    Task DeleteAsync(string relativePath, CancellationToken ct);
    Task DeleteDirectoryAsync(string relativePath, CancellationToken ct);
}
