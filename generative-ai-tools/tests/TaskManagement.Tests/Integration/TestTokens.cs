using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace TaskManagement.Tests.Integration;

public static class TestTokens
{
    private static readonly JsonWebTokenHandler Handler = new();
    private static readonly TimeSpan ValidFor = TimeSpan.FromMinutes(10);

    public static string For(Guid userId) => WithSubject(userId.ToString());

    public static string WithSubject(string? subject) =>
        Create(subject, TestJwtSettings.Issuer, TestJwtSettings.Audience, TestJwtSettings.SigningKey, DateTime.UtcNow + ValidFor);

    public static string Expired(Guid userId) =>
        Create(userId.ToString(), TestJwtSettings.Issuer, TestJwtSettings.Audience, TestJwtSettings.SigningKey, DateTime.UtcNow.AddMinutes(-10));

    public static string SignedWith(Guid userId, string signingKey) =>
        Create(userId.ToString(), TestJwtSettings.Issuer, TestJwtSettings.Audience, signingKey, DateTime.UtcNow + ValidFor);

    public static string IssuedBy(Guid userId, string issuer) =>
        Create(userId.ToString(), issuer, TestJwtSettings.Audience, TestJwtSettings.SigningKey, DateTime.UtcNow + ValidFor);

    public static string IssuedFor(Guid userId, string audience) =>
        Create(userId.ToString(), TestJwtSettings.Issuer, audience, TestJwtSettings.SigningKey, DateTime.UtcNow + ValidFor);

    public static string Unsigned(Guid userId) =>
        Create(userId.ToString(), TestJwtSettings.Issuer, TestJwtSettings.Audience, null, DateTime.UtcNow + ValidFor);

    public static string NotYetValid(Guid userId) =>
        Create(userId.ToString(), TestJwtSettings.Issuer, TestJwtSettings.Audience, TestJwtSettings.SigningKey, DateTime.UtcNow.AddMinutes(70));

    public static string WithoutExpiry(Guid userId)
    {
        var handler = new JsonWebTokenHandler { SetDefaultTimesOnTokenCreation = false };
        return handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = TestJwtSettings.Issuer,
            Audience = TestJwtSettings.Audience,
            Claims = new Dictionary<string, object> { ["sub"] = userId.ToString() },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSettings.SigningKey)), SecurityAlgorithms.HmacSha256),
        });
    }

    private static string Create(string? subject, string issuer, string audience, string? signingKey, DateTime expires)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Expires = expires,
            NotBefore = expires.AddHours(-1),
            IssuedAt = expires.AddHours(-1),
            Claims = subject is null ? new Dictionary<string, object>() : new Dictionary<string, object> { ["sub"] = subject },
            SigningCredentials = signingKey is null
                ? null
                : new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)), SecurityAlgorithms.HmacSha256),
        };
        return Handler.CreateToken(descriptor);
    }
}
