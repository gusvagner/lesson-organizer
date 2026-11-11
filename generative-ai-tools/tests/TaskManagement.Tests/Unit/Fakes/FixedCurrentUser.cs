using TaskManagement.Api.Auth;

namespace TaskManagement.Tests.Unit.Fakes;

public sealed class FixedCurrentUser(Guid userId) : ICurrentUser
{
    public Guid UserId { get; } = userId;
}
