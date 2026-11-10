using LessonOrganizer.Application.Abstractions;
using LessonOrganizer.Domain.Common;

namespace LessonOrganizer.Application.Auth;

public sealed class AuthService(IUserAccounts accounts)
{
    public const int PasswordMinLength = 8;
    public const int PasswordMaxLength = 128;
    public const int NameMaxLength = 150;

    private static readonly Error InvalidCredentials = Error.Unauthorized("Invalid email or password.");
    private static readonly Error PasswordRequired = Error.Validation("password", "Password is required.");
    private static readonly Error UserNotFound = Error.NotFound("User not found.");

    public async Task<Result<UserResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var errors = new List<Error>();
        var name = TextField.Required(request.Name, "name", "Name", NameMaxLength, errors);
        var email = EmailField.Required(request.Email, errors);
        CheckPassword(request.Password, errors);
        if (errors.Count > 0)
        {
            return Result<UserResponse>.Failure(errors);
        }

        return await accounts.CreateAsync(name!, email!, request.Password!, ct);
    }

    public async Task<Result<UserResponse>> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var errors = new List<Error>();
        var email = TextField.Required(request.Email, "email", "Email", EmailField.MaxLength, errors);
        if (string.IsNullOrEmpty(request.Password))
        {
            errors.Add(PasswordRequired);
        }

        if (errors.Count > 0)
        {
            return Result<UserResponse>.Failure(errors);
        }

        var outcome = await accounts.CheckPasswordAsync(email!, request.Password!, ct);
        return outcome is { Status: LoginStatus.Succeeded, User: { } user }
            ? Result<UserResponse>.Success(user)
            : Result<UserResponse>.Failure(InvalidCredentials);
    }

    public async Task<Result<UserResponse>> GetAsync(Guid userId, CancellationToken ct)
    {
        var user = await accounts.FindByIdAsync(userId, ct);
        return user is null ? Result<UserResponse>.Failure(UserNotFound) : Result<UserResponse>.Success(user);
    }

    private static void CheckPassword(string? password, List<Error> errors)
    {
        if (string.IsNullOrEmpty(password))
        {
            errors.Add(PasswordRequired);
            return;
        }

        if (password.Length < PasswordMinLength)
        {
            errors.Add(Error.Validation("password", $"Password must be at least {PasswordMinLength} characters."));
        }

        if (password.Length > PasswordMaxLength)
        {
            errors.Add(Error.Validation("password", $"Password must be at most {PasswordMaxLength} characters."));
        }
    }
}
