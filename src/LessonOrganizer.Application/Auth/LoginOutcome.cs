namespace LessonOrganizer.Application.Auth;

public enum LoginStatus
{
    Succeeded,
    InvalidCredentials,
    LockedOut,
}

public sealed record LoginOutcome(LoginStatus Status, UserResponse? User);
