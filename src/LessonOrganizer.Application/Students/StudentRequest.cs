using LessonOrganizer.Domain.Students;

namespace LessonOrganizer.Application.Students;

public sealed record StudentRequest(string? Name, string? Email, EnglishLevel? EnglishLevel, string? Notes);
