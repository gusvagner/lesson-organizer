namespace LessonOrganizer.Api.Http;

public static class SecurityHeaders
{
    private const string ContentSecurityPolicy =
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; " +
        "font-src 'self'; connect-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'; " +
        "frame-ancestors 'none'";

    public static void UseSecurityHeaders(this WebApplication app) =>
        app.Use((context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                AddHeaders(context.Response.Headers);
                return Task.CompletedTask;
            });
            return next(context);
        });

    private static void AddHeaders(IHeaderDictionary headers)
    {
        headers.XContentTypeOptions = "nosniff";
        headers.XFrameOptions = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers.ContentSecurityPolicy = ContentSecurityPolicy;
    }
}
