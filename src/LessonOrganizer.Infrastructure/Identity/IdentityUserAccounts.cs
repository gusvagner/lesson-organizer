using LessonOrganizer.Application.Abstractions;
using LessonOrganizer.Application.Auth;
using LessonOrganizer.Domain.Common;
using LessonOrganizer.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LessonOrganizer.Infrastructure.Identity;

public sealed class IdentityUserAccounts(UserManager<ApplicationUser> users, IPasswordHasher<ApplicationUser> hasher) : IUserAccounts
{
    private static readonly string DummyPasswordHash =
        new PasswordHasher<ApplicationUser>().HashPassword(new ApplicationUser(), Guid.NewGuid().ToString());

    private static readonly Error EmailTaken = Error.Conflict("An account with this email already exists.");
    private static readonly string[] DuplicateCodes = ["DuplicateUserName", "DuplicateEmail"];

    public async Task<Result<UserResponse>> CreateAsync(string name, string email, string password, CancellationToken ct)
    {
        var user = new ApplicationUser { Name = name, Email = email, UserName = email };
        try
        {
            var created = await users.CreateAsync(user, password);
            return created.Succeeded ? Result<UserResponse>.Success(ToResponse(user)) : Result<UserResponse>.Failure(ToErrors(created));
        }
        catch (DbUpdateException ex) when (SqlErrors.IsDuplicateKey(ex))
        {
            return Result<UserResponse>.Failure(EmailTaken);
        }
    }

    public async Task<UserResponse?> FindByIdAsync(Guid id, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(id.ToString());
        return user is null ? null : ToResponse(user);
    }

    public async Task<LoginOutcome> CheckPasswordAsync(string email, string password, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(email);
        if (user is null)
        {
            hasher.VerifyHashedPassword(new ApplicationUser(), DummyPasswordHash, password);
            return new LoginOutcome(LoginStatus.InvalidCredentials, null);
        }

        if (await users.IsLockedOutAsync(user))
        {
            hasher.VerifyHashedPassword(user, user.PasswordHash ?? DummyPasswordHash, password);
            return new LoginOutcome(LoginStatus.LockedOut, null);
        }

        return await CheckUnlockedAsync(user, password);
    }

    private async Task<LoginOutcome> CheckUnlockedAsync(ApplicationUser user, string password)
    {
        if (await users.CheckPasswordAsync(user, password))
        {
            await users.ResetAccessFailedCountAsync(user);
            return new LoginOutcome(LoginStatus.Succeeded, ToResponse(user));
        }

        await users.AccessFailedAsync(user);
        var status = await users.IsLockedOutAsync(user) ? LoginStatus.LockedOut : LoginStatus.InvalidCredentials;
        return new LoginOutcome(status, null);
    }

    private static List<Error> ToErrors(IdentityResult result)
    {
        if (result.Errors.Any(error => DuplicateCodes.Contains(error.Code)))
        {
            return [EmailTaken];
        }

        return result.Errors
            .Select(error => Error.Validation(error.Code.StartsWith("Password", StringComparison.Ordinal) ? "password" : "email", error.Description))
            .ToList();
    }

    private static UserResponse ToResponse(ApplicationUser user) => new(user.Id, user.Name, user.Email!);
}
