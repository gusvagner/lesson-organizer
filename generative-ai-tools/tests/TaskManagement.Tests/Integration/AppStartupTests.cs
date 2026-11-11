using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TaskManagement.Api.Auth;

namespace TaskManagement.Tests.Integration;

public sealed class AppStartupTests
{
    [Fact]
    public async Task Start_WithValidJwtSettings_ServesHttpRequests()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/no-such-endpoint", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Start_WithValidJwtSettings_BindsJwtOptionsFromConfiguration()
    {
        await using var factory = new ApiFactory();

        var options = factory.Services.GetRequiredService<IOptions<JwtOptions>>().Value;

        Assert.Equal(TestJwtSettings.Issuer, options.Issuer);
        Assert.Equal(TestJwtSettings.Audience, options.Audience);
        Assert.Equal(TestJwtSettings.SigningKey, options.SigningKey);
    }

    [Fact]
    public async Task Start_WithValidJwtSettings_RegistersSystemTimeProvider()
    {
        await using var factory = new ApiFactory();

        var timeProvider = factory.Services.GetRequiredService<TimeProvider>();

        Assert.Same(TimeProvider.System, timeProvider);
    }

    [Fact]
    public async Task Start_WithSigningKeyShorterThan32Bytes_FailsOptionsValidation()
    {
        await using var factory = new ApiFactory(TestJwtSettings.With("Jwt:SigningKey", new string('k', 31)));

        var exception = Assert.Throws<OptionsValidationException>(() => factory.Services);

        Assert.Contains("Jwt:SigningKey", exception.Message);
    }

    [Fact]
    public async Task Start_WithoutIssuer_FailsOptionsValidation()
    {
        await using var factory = new ApiFactory(TestJwtSettings.With("Jwt:Issuer", ""));

        var exception = Assert.Throws<OptionsValidationException>(() => factory.Services);

        Assert.Contains("Jwt:Issuer", exception.Message);
    }
}
