namespace LessonOrganizer.Api.Http;

public static class SecurityHeaders
{
    private const string ContentSecurityPolicy =
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; " +
        "font-src 'self'; connect-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'; " +
        "frame-ancestors 'none'";

    private static readonly PathString[] DevelopmentDocsPaths = ["/scalar", "/openapi"];

    public static void UseSecurityHeaders(this WebApplication app)
    {
        var isDevelopment = app.Environment.IsDevelopment();
        app.Use((context, next) =>
        {
            var withPolicy = !(isDevelopment && IsDocsPath(context.Request.Path));
            context.Response.OnStarting(() =>
            {
                AddHeaders(context.Response.Headers, withPolicy);
                return Task.CompletedTask;
            });
            return next(context);
        });
    }

    private static bool IsDocsPath(PathString path) =>
        DevelopmentDocsPaths.Any(docsPath => path.StartsWithSegments(docsPath, StringComparison.OrdinalIgnoreCase));

    private static void AddHeaders(IHeaderDictionary headers, bool withPolicy)
    {
        headers.XContentTypeOptions = "nosniff";
        headers.XFrameOptions = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        if (withPolicy)
        {
            headers.ContentSecurityPolicy = ContentSecurityPolicy;
        }
    }
}
