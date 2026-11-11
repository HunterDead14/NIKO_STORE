namespace Api.Settings;

// Strongly-typed налаштування застосунку (розд. 10 ТЗ). Прив'язується до кореня конфігурації,
// тож ключі збігаються з appsettings.json і змінними оточення (ConnectionStrings__DefaultConnection).
public sealed class ApplicationSettings
{
    public ConnectionStringsSettings ConnectionStrings { get; init; } = new();
    public StorageSettings Storage { get; init; } = new();

    // Необов'язкова база для абсолютних посилань (клієнт або майбутній CDN).
    // У imageUrl не входить — той завжди відносний (розд. 7.3 ТЗ).
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
