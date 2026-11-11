using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Data;
using TaskManagement.Api.Tasks;
using TaskManagement.Api.Users;

namespace TaskManagement.Tests.Integration;

public sealed class TestDatabase(string connectionString)
{
    public string ConnectionString { get; } = connectionString;

    public TaskManagementDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<TaskManagementDbContext>().UseSqlServer(ConnectionString).Options);

    public async Task<User> SeedUserAsync(CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var user = new User(id, $"{id:N}@example.test", "Test user");
        await using var context = CreateContext();
        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task SeedTasksAsync(CancellationToken cancellationToken, params TaskItem[] tasks)
    {
        await using var context = CreateContext();
        context.Tasks.AddRange(tasks);
        await context.SaveChangesAsync(cancellationToken);
    }
}
