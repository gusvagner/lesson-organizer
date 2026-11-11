using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Tasks;
using TaskManagement.Api.Users;

namespace TaskManagement.Api.Data;

public sealed class TaskManagementDbContext(DbContextOptions<TaskManagementDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TaskManagementDbContext).Assembly);
}
