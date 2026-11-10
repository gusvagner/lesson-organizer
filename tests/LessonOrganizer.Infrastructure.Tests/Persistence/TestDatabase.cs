using LessonOrganizer.Testing.SqlServer;
using LessonOrganizer.Infrastructure.Identity;
using LessonOrganizer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LessonOrganizer.Infrastructure.Tests.Persistence;

public sealed class TestDatabase : IAsyncDisposable
{
    private readonly DbContextOptions<LessonOrganizerDbContext> _options;

    private TestDatabase(string connectionString)
    {
        _options = new DbContextOptionsBuilder<LessonOrganizerDbContext>().UseSqlServer(connectionString).Options;
    }

    public static async Task<TestDatabase> CreateAsync(SqlServerFixture sql)
    {
        var database = new TestDatabase(sql.ConnectionStringFor($"test_{Guid.NewGuid():N}"));
        await using var context = database.NewContext();
        await context.Database.MigrateAsync();
        return database;
    }

    public LessonOrganizerDbContext NewContext() => new(_options);

    public async Task<Guid> AddTeacherAsync(string email = "teacher@example.com")
    {
        var teacher = new ApplicationUser
        {
            Id = Guid.CreateVersion7(),
            Name = "Teacher",
            Email = email,
            UserName = email,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant(),
        };
        await using var context = NewContext();
        context.Users.Add(teacher);
        await context.SaveChangesAsync();
        return teacher.Id;
    }

    public async ValueTask DisposeAsync()
    {
        await using var context = NewContext();
        await context.Database.EnsureDeletedAsync();
    }
}
