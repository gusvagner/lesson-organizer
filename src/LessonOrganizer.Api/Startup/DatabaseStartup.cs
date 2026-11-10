using LessonOrganizer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LessonOrganizer.Api.Startup;

public static class DatabaseStartup
{
    public static async Task MigrateIfConfiguredAsync(WebApplication app)
    {
        if (!app.Configuration.GetValue("Database:MigrateOnStartup", false))
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<LessonOrganizerDbContext>().Database.MigrateAsync();
    }
}
