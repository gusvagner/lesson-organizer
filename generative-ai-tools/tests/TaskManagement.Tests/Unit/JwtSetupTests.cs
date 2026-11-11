using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TaskManagement.Api.Auth;

namespace TaskManagement.Tests.Unit;

public sealed class JwtSetupTests
{
    [Fact]
    public void AddValidatedJwtOptions_CalledTwice_RegistersValidatorOnce()
    {
        var services = new ServiceCollection();

        services.AddValidatedJwtOptions();
        services.AddValidatedJwtOptions();

        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IValidateOptions<JwtOptions>));
    }
}
