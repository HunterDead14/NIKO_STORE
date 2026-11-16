using Api.Modules.Errors;

namespace Api.Modules;

public static class SetupModule
{
    public static IServiceCollection SetupServices(this IServiceCollection services)
    {
        services.AddControllers(options =>
            options.Filters.Add<ValidationExceptionFilter>());

        // Єдиний контракт помилок (розд. 8 ТЗ): ProblemDetails і для 500,
        // і для «порожніх» відповідей на кшталт 404 невідомого маршруту чи 405.
        services.AddProblemDetails(options =>
            options.CustomizeProblemDetails = context =>
                context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier);
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.AddCors(options =>
            options.AddDefaultPolicy(policy =>
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .WithExposedHeaders("Location", "ETag")));

        return services;
    }

    public static WebApplication UseErrorHandling(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        return app;
    }
}
