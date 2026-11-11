using System.Text;
using Microsoft.Extensions.Options;

namespace TaskManagement.Api.Auth;

public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public const int MinimumSigningKeyBytes = 32;

    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        var failures = new List<string>();
        AddFailureIfBlank(failures, nameof(JwtOptions.Issuer), options.Issuer);
        AddFailureIfBlank(failures, nameof(JwtOptions.Audience), options.Audience);
        AddFailureIfSigningKeyWeak(failures, options.SigningKey);

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static void AddFailureIfBlank(List<string> failures, string setting, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            failures.Add($"{JwtOptions.SectionName}:{setting} is required; got '{value}'.");
        }
    }

    private static void AddFailureIfSigningKeyWeak(List<string> failures, string signingKey)
    {
        var setting = $"{JwtOptions.SectionName}:{nameof(JwtOptions.SigningKey)}";
        var keyBytes = Encoding.UTF8.GetByteCount(signingKey);
        if (keyBytes < MinimumSigningKeyBytes)
        {
            failures.Add($"{setting} must be at least {MinimumSigningKeyBytes} bytes in UTF-8; got {keyBytes} bytes.");
            return;
        }

        if (string.IsNullOrWhiteSpace(signingKey))
        {
            failures.Add($"{setting} must not be only whitespace; got {keyBytes} bytes of whitespace.");
        }
    }
}
