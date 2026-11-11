using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TaskManagement.Api.Data;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TaskManagementDbContext>
{
    private const string MigrationsOnlyConnectionString = "Server=design-time-only;Database=TaskManagement";

    public TaskManagementDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<TaskManagementDbContext>().UseSqlServer(MigrationsOnlyConnectionString).Options);
}
