using LessonOrganizer.Application.Auth;
using LessonOrganizer.Domain.Common;

namespace LessonOrganizer.Application.Abstractions;

public interface IUserAccounts
{
    Task<Result<UserResponse>> CreateAsync(string name, string email, string password, CancellationToken ct);

    Task<UserResponse?> FindByIdAsync(Guid id, CancellationToken ct);

    Task<LoginOutcome> CheckPasswordAsync(string email, string password, CancellationToken ct);
}
