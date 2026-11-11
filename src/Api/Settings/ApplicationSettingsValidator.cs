using Microsoft.Extensions.Options;

namespace Api.Settings;

// Перевіряється при старті (ValidateOnStart): з неповною конфігурацією застосунок
// падає одразу з зрозумілим повідомленням, а не на першому запиті.
public sealed class ApplicationSettingsValidator : IValidateOptions<ApplicationSettings>
{
    public ValidateOptionsResult Validate(string? name, ApplicationSettings options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ConnectionStrings.DefaultConnection))
        {
            failures.Add("'ConnectionStrings:DefaultConnection' is not configured.");
        }

        if (string.IsNullOrWhiteSpace(options.Storage.RootPath))
        {
            failures.Add("'Storage:RootPath' is not configured.");
        }

        if (!string.IsNullOrWhiteSpace(options.PublicBaseUrl) && !IsAbsoluteHttpUrl(options.PublicBaseUrl))
        {
            failures.Add($"'PublicBaseUrl' must be an absolute http(s) URL, got '{options.PublicBaseUrl}'.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static bool IsAbsoluteHttpUrl(string value)
        => Uri.TryCreate(value, UriKind.Absolute, out var uri)
           && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
