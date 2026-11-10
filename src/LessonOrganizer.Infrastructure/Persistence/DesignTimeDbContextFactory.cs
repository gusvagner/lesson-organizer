using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LessonOrganizer.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LessonOrganizerDbContext>
{
    private const string PlaceholderConnectionString =
        "Server=localhost;Database=LessonOrganizer;Integrated Security=false;TrustServerCertificate=true";

    public LessonOrganizerDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__LessonOrganizer")
            ?? PlaceholderConnectionString;
        var options = new DbContextOptionsBuilder<LessonOrganizerDbContext>().UseSqlServer(connectionString).Options;
        return new LessonOrganizerDbContext(options);
    }
}
