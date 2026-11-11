using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace TaskManagement.Api.Auth;

public static class JwtSetup
{
    public static IServiceCollection AddValidatedJwtOptions(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>());
        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .ValidateOnStart();
        return services;
    }
}
