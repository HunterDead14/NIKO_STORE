using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class ConfigureApplicationServices
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(ConfigureApplicationServices).Assembly));

        services.AddValidatorsFromAssembly(typeof(ConfigureApplicationServices).Assembly);

        // ValidationBehaviour (IPipelineBehavior) реєструється на Кроці 4 разом із валідаторами.

        return services;
    }
}
