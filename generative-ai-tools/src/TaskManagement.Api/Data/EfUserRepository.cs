using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Users;

namespace TaskManagement.Api.Data;

public sealed class EfUserRepository(TaskManagementDbContext dbContext) : IUserRepository
{
    public Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.Users.AnyAsync(user => user.Id == userId, cancellationToken);
}
