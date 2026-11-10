using LessonOrganizer.Infrastructure.Persistence;
using LessonOrganizer.Infrastructure.Seed;
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
        await SeedIfConfiguredAsync(app, scope.ServiceProvider);
    }

    private static async Task SeedIfConfiguredAsync(WebApplication app, IServiceProvider services)
    {
        if (app.Configuration.GetValue("Seed:DemoData", false))
        {
            await services.GetRequiredService<DemoDataSeeder>().SeedAsync(CancellationToken.None);
        }
    }
}
