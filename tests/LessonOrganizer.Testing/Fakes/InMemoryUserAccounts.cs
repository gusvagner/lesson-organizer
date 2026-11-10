using LessonOrganizer.Application.Abstractions;
using LessonOrganizer.Application.Auth;
using LessonOrganizer.Domain.Common;

namespace LessonOrganizer.Testing.Fakes;

public sealed class InMemoryUserAccounts : IUserAccounts
{
    private const int MaxFailedAttempts = 5;

    private readonly List<Account> _accounts = [];

    public IReadOnlyList<UserResponse> Users => _accounts.Select(account => account.User).ToList();

    public Task<Result<UserResponse>> CreateAsync(string name, string email, string password, CancellationToken ct)
    {
        if (Find(email) is not null)
        {
            return Task.FromResult(Result<UserResponse>.Failure(Error.Conflict("An account with this email already exists.")));
        }

        var user = new UserResponse(Guid.CreateVersion7(), name, email);
        _accounts.Add(new Account(user, password));
        return Task.FromResult(Result<UserResponse>.Success(user));
    }

    public Task<UserResponse?> FindByIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(_accounts.SingleOrDefault(account => account.User.Id == id)?.User);

    public Task<LoginOutcome> CheckPasswordAsync(string email, string password, CancellationToken ct) =>
        Task.FromResult(Check(Find(email), password));

    public void Unlock(string email) => Find(email)!.FailedAttempts = 0;

    private static LoginOutcome Check(Account? account, string password)
    {
        if (account is null)
        {
            return new LoginOutcome(LoginStatus.InvalidCredentials, null);
        }

        if (account.IsLocked)
        {
            return new LoginOutcome(LoginStatus.LockedOut, null);
        }

        if (account.Password == password)
        {
            account.FailedAttempts = 0;
            return new LoginOutcome(LoginStatus.Succeeded, account.User);
        }

        account.FailedAttempts++;
        return new LoginOutcome(account.IsLocked ? LoginStatus.LockedOut : LoginStatus.InvalidCredentials, null);
    }

    private Account? Find(string email) =>
        _accounts.SingleOrDefault(account => string.Equals(account.User.Email, email, StringComparison.OrdinalIgnoreCase));

    private sealed class Account(UserResponse user, string password)
    {
        public UserResponse User { get; } = user;

        public string Password { get; } = password;

        public int FailedAttempts { get; set; }

        public bool IsLocked => FailedAttempts >= MaxFailedAttempts;
    }
}
