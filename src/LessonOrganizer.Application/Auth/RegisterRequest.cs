namespace LessonOrganizer.Application.Auth;

public sealed record RegisterRequest(string? Name, string? Email, string? Password);
