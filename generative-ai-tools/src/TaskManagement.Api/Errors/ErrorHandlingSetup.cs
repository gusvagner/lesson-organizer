using Microsoft.AspNetCore.Mvc;

namespace TaskManagement.Api.Errors;

public static class ErrorHandlingSetup
{
    public static IServiceCollection AddApiProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.Configure<ApiBehaviorOptions>(options =>
            options.InvalidModelStateResponseFactory = ValidationProblemResponse.Create);
        return services;
    }

    public static WebApplication UseApiErrorHandling(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler();
        }

        app.UseStatusCodePages();
        return app;
    }
}
