using Microsoft.AspNetCore.StaticFiles;

namespace LessonOrganizer.Api.Startup;

/// <summary>
/// Serves the built Angular app from wwwroot on the API's origin. Unknown /api paths stay API
/// responses (401 without a session, 404 with one); every other unknown path returns index.html.
/// </summary>
public static class SpaHosting
{
    private const string IndexFile = "index.html";

    public static void UseSpaStaticFiles(this WebApplication app) =>
        app.UseStaticFiles(new StaticFileOptions { OnPrepareResponse = RevalidateIndex });

    public static void MapSpaFallback(this WebApplication app)
    {
        app.MapFallback("api/{**path}", () => Results.Problem(statusCode: StatusCodes.Status404NotFound));
        app.MapFallbackToFile(IndexFile, new StaticFileOptions { OnPrepareResponse = RevalidateIndex })
            .AllowAnonymous();
    }

    private static void RevalidateIndex(StaticFileResponseContext context)
    {
        if (context.File.Name == IndexFile)
        {
            context.Context.Response.Headers.CacheControl = "no-cache";
        }
    }
}
