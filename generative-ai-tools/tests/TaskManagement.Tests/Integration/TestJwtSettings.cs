namespace TaskManagement.Tests.Integration;

public static class TestJwtSettings
{
    public const string Issuer = "https://tests.task-management.local";
    public const string Audience = "task-management-api-tests";
    public const string SigningKey = "test-signing-key-with-at-least-32-bytes-0123456789";

    public static IReadOnlyDictionary<string, string?> Valid { get; } = new Dictionary<string, string?>
    {
        ["Jwt:Issuer"] = Issuer,
        ["Jwt:Audience"] = Audience,
        ["Jwt:SigningKey"] = SigningKey,
    };

    public static IReadOnlyDictionary<string, string?> With(string key, string? value) =>
        new Dictionary<string, string?>(Valid) { [key] = value };
}
