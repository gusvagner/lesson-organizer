using TaskManagement.Api.Auth;

namespace TaskManagement.Tests.Unit;

public sealed class JwtOptionsValidatorTests
{
    private readonly JwtOptionsValidator _validator = new();

    [Fact]
    public void Validate_CompleteOptionsWith32ByteKey_Succeeds()
    {
        var result = _validator.Validate(null, ValidOptions(signingKey: new string('k', 32)));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_SigningKeyOf31Bytes_FailsNamingKeyAndLength()
    {
        var result = _validator.Validate(null, ValidOptions(signingKey: new string('k', 31)));

        Assert.True(result.Failed);
        var failure = Assert.Single(result.Failures!);
        Assert.Contains("Jwt:SigningKey", failure);
        Assert.Contains("31 bytes", failure);
    }

    [Fact]
    public void Validate_SigningKeyOf16TwoByteCharacters_CountsUtf8BytesAndSucceeds()
    {
        var result = _validator.Validate(null, ValidOptions(signingKey: new string('é', 16)));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_SigningKeyMissing_Fails()
    {
        var result = _validator.Validate(null, ValidOptions(signingKey: ""));

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, failure => failure.Contains("Jwt:SigningKey"));
    }

    [Fact]
    public void Validate_SigningKeyOf32Spaces_Fails()
    {
        var result = _validator.Validate(null, ValidOptions(signingKey: new string(' ', 32)));

        Assert.True(result.Failed);
        var failure = Assert.Single(result.Failures!);
        Assert.Contains("Jwt:SigningKey", failure);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_BlankIssuer_Fails(string issuer)
    {
        var options = ValidOptions();
        options.Issuer = issuer;

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        var failure = Assert.Single(result.Failures!);
        Assert.Contains("Jwt:Issuer", failure);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_BlankAudience_Fails(string audience)
    {
        var options = ValidOptions();
        options.Audience = audience;

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        var failure = Assert.Single(result.Failures!);
        Assert.Contains("Jwt:Audience", failure);
    }

    private static JwtOptions ValidOptions(string signingKey = "0123456789abcdef0123456789abcdef") => new()
    {
        Issuer = "https://issuer.example",
        Audience = "task-management-api",
        SigningKey = signingKey,
    };
}
