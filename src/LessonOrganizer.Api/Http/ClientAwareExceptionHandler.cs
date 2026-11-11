using Microsoft.AspNetCore.Diagnostics;

namespace LessonOrganizer.Api.Http;

/// <summary>
/// Exception handler for non-development hosts. A bad request found while reading the body (for example a
/// chunked body over the size limit) keeps its own status code instead of becoming a 500, and is not
/// logged as a server error; anything else is a 500 ProblemDetails without details.
/// </summary>
public static class ClientAwareExceptionHandler
{
    public static void UseClientAwareExceptionHandler(this WebApplication app) =>
        app.UseExceptionHandler(new ExceptionHandlerOptions
        {
            StatusCodeSelector = StatusCodeFor,
            SuppressDiagnosticsCallback = context => context.Exception is BadHttpRequestException,
        });

    private static int StatusCodeFor(Exception exception) =>
        exception is BadHttpRequestException badRequest ? badRequest.StatusCode : StatusCodes.Status500InternalServerError;
}
