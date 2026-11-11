namespace LessonOrganizer.Api.Http;

/// <summary>
/// Rejects a request whose declared body is larger than the limit with a 413 ProblemDetails before
/// anything reads it. Kestrel's MaxRequestBodySize, set to the same value, also stops chunked bodies.
/// </summary>
public static class RequestBodyLimit
{
    public const long MaxBytes = 1024 * 1024;

    public static void UseRequestBodyLimit(this WebApplication app) =>
        app.Use(async (context, next) =>
        {
            if (context.Request.ContentLength > MaxBytes)
            {
                await RejectAsync(context);
                return;
            }
            await next(context);
        });

    private static Task RejectAsync(HttpContext context) =>
        Results.Problem(
                statusCode: StatusCodes.Status413PayloadTooLarge,
                detail: $"The request body is larger than {MaxBytes / 1024} KB.")
            .ExecuteAsync(context);
}
