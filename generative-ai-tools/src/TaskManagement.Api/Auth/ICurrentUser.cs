namespace TaskManagement.Api.Auth;

public interface ICurrentUser
{
    Guid UserId { get; }
}
