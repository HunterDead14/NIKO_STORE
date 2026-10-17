using Application.Common.Interfaces.Services;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Infrastructure.Services;

public class LocalFileStorage : IFileStorage
{
    private readonly string _rootPath;

    public LocalFileStorage(string rootPath)
    {
        _rootPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(rootPath));
    }

    public async Task SaveAsync(string relativePath, Stream content, CancellationToken ct)
    {
        var fullPath = ResolvePath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var file = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(file, ct);
    }

    public Task<Option<Stream>> OpenReadAsync(string relativePath, CancellationToken ct)
    {
        var fullPath = ResolvePath(relativePath);

        var result = File.Exists(fullPath)
            ? Some<Stream>(new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            : None;

        return Task.FromResult(result);
    }

    public Task DeleteAsync(string relativePath, CancellationToken ct)
    {
        File.Delete(ResolvePath(relativePath));
        return Task.CompletedTask;
    }

    public Task DeleteDirectoryAsync(string relativePath, CancellationToken ct)
    {
        var fullPath = ResolvePath(relativePath);

        if (Directory.Exists(fullPath))
        {
            Directory.Delete(fullPath, recursive: true);
        }

        return Task.CompletedTask;
    }

    // Захист від path traversal: приймаємо лише відносний шлях,
    // і результат мусить лежати строго всередині кореня сховища.
    private string ResolvePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
        {
            throw new ArgumentException($"Storage path must be relative: '{relativePath}'.", nameof(relativePath));
        }

        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, relativePath));

        if (!fullPath.StartsWith(_rootPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Storage path escapes the storage root: '{relativePath}'.", nameof(relativePath));
        }

        return fullPath;
    }
}
