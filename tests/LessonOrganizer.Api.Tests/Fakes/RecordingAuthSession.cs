using LessonOrganizer.Api.Security;

namespace LessonOrganizer.Api.Tests.Fakes;

public sealed class RecordingAuthSession : IAuthSession
{
    public Guid? SignedInUserId { get; private set; }

    public bool SignedOut { get; private set; }

    public bool IsSignedIn { get; set; } = true;

    public Task SignInAsync(Guid userId)
    {
        SignedInUserId = userId;
        return Task.CompletedTask;
    }

    public Task SignOutAsync()
    {
        SignedOut = true;
        return Task.CompletedTask;
    }
}
