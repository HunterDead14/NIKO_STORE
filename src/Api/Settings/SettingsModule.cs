using Microsoft.Extensions.Options;

namespace Api.Settings;

public static class SettingsModule
{
    public static IServiceCollection AddApplicationSettings(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<ApplicationSettings>, ApplicationSettingsValidator>();

        services.AddOptions<ApplicationSettings>()
            .Bind(configuration)
            .ValidateOnStart();

        return services;
    }
}
