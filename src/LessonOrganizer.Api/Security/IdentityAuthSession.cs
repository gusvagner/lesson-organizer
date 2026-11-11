using LessonOrganizer.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace LessonOrganizer.Api.Security;

public sealed class IdentityAuthSession(SignInManager<ApplicationUser> signIn, UserManager<ApplicationUser> users) : IAuthSession
{
    public async Task SignInAsync(Guid userId)
    {
        var user = await users.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException($"User {userId} does not exist. Expected an existing user id to sign in.");
        await signIn.SignInAsync(user, isPersistent: true);
    }

    public bool IsSignedIn => signIn.IsSignedIn(signIn.Context.User);

    public Task SignOutAsync() => signIn.SignOutAsync();
}
