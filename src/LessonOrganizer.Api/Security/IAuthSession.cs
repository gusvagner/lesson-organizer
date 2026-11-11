namespace LessonOrganizer.Api.Security;

public interface IAuthSession
{
    Task SignInAsync(Guid userId);

    bool IsSignedIn { get; }

    Task SignOutAsync();
}
