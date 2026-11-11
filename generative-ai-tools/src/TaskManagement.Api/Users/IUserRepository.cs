namespace TaskManagement.Api.Users;

public interface IUserRepository
{
    Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken);
}
