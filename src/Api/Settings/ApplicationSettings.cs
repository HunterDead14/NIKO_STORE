namespace Api.Settings;

public sealed class ApplicationSettings
{
    public ConnectionStringsSettings ConnectionStrings { get; init; } = new();
    public StorageSettings Storage { get; init; } = new();

    public string? PublicBaseUrl { get; init; }
}

public sealed class ConnectionStringsSettings
{
    public string DefaultConnection { get; init; } = string.Empty;
}

public sealed class StorageSettings
{
    public string RootPath { get; init; } = string.Empty;
}
