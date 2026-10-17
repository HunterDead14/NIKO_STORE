using Application.Common.Interfaces.Services;
using Infrastructure.Persistence;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class ConfigureInfrastructureServices
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPersistenceServices(configuration);

        services.AddScoped<INotificationService, LoggingNotificationService>();

        // IFileStorage реєструється на Кроці 7.

        return services;
    }
}
