namespace Api.Modules;

public static class SetupModule
{
    public static IServiceCollection SetupServices(this IServiceCollection services)
    {
        services.AddControllers();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.AddCors(options =>
            options.AddDefaultPolicy(policy =>
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod()));

        return services;
    }
}
