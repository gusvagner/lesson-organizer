namespace LessonOrganizer.Api.IntegrationTests.Security;

public static class SecurityHeaderAssertions
{
    public const string ContentSecurityPolicy =
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; " +
        "font-src 'self'; connect-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'; " +
        "frame-ancestors 'none'";

    public static void AssertSecurityHeaders(HttpResponseMessage response)
    {
        Assert.Equal("nosniff", HeaderValue(response, "X-Content-Type-Options"));
        Assert.Equal("DENY", HeaderValue(response, "X-Frame-Options"));
        Assert.Equal("no-referrer", HeaderValue(response, "Referrer-Policy"));
        Assert.Equal(ContentSecurityPolicy, HeaderValue(response, "Content-Security-Policy"));
    }

    public static string? HeaderValue(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out var values) ? string.Join(",", values) : null;
}
