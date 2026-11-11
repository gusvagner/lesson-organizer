using Microsoft.AspNetCore.Mvc;

namespace TaskManagement.Api.Http;

public static class HttpSetup
{
    public static IServiceCollection AddApiControllers(this IServiceCollection services)
    {
        services.AddControllers(options => options.ModelBinderProviders.Insert(0, new EnumNameModelBinderProvider()))
            .AddJsonOptions(options => ConfigureJson(options));
        return services;
    }

    private static void ConfigureJson(JsonOptions options)
    {
        options.AllowInputFormatterExceptionMessages = false;
        options.JsonSerializerOptions.Converters.Add(new EnumNameJsonConverterFactory());
    }
}
