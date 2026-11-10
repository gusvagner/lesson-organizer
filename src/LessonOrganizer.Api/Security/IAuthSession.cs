namespace LessonOrganizer.Api.Security;

public interface IAuthSession
{
    Task SignInAsync(Guid userId);

    Task SignOutAsync();
}
