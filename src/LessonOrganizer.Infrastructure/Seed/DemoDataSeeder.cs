using LessonOrganizer.Application.Abstractions;
using LessonOrganizer.Domain.Common;
using LessonOrganizer.Domain.LessonPlans;
using LessonOrganizer.Domain.Students;
using LessonOrganizer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LessonOrganizer.Infrastructure.Seed;

public sealed class DemoDataSeeder(
    LessonOrganizerDbContext db,
    IUserAccounts accounts,
    IStudentRepository students,
    ILessonPlanRepository lessonPlans,
    TimeProvider time)
{
    public const string DemoPassword = "Demo1234!";

    private static readonly SeedTeacher[] Teachers =
    [
        new("Maria Silva", "maria@example.com",
        [
            new("Ana", EnglishLevel.Intermediate,
            [
                new("Present perfect — conversation", LessonPlanStatus.Delivered, -7),
                new("Phrasal verbs at work", LessonPlanStatus.Ready, 2),
            ]),
            new("Bruno", EnglishLevel.Beginner, [new("Greetings and introductions", LessonPlanStatus.ToPrepare, 5)]),
            new("Carla", EnglishLevel.Advanced, []),
        ]),
        new("John Smith", "john@example.com",
        [
            new("David", EnglishLevel.Elementary,
            [
                new("Past simple stories", LessonPlanStatus.Delivered, -3),
                new("Ordering at a restaurant", LessonPlanStatus.ToPrepare, 4),
            ]),
            new("Emma", EnglishLevel.UpperIntermediate, [new("Job interview practice", LessonPlanStatus.Ready, 1)]),
        ]),
    ];

    public async Task<bool> SeedAsync(CancellationToken ct)
    {
        if (await db.Users.AnyAsync(ct))
        {
            return false;
        }

        var today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        foreach (var teacher in Teachers)
        {
            await SeedTeacherAsync(teacher, today, ct);
        }

        return true;
    }

    private async Task SeedTeacherAsync(SeedTeacher teacher, DateOnly today, CancellationToken ct)
    {
        var user = Created(await accounts.CreateAsync(teacher.Name, teacher.Email, DemoPassword, ct), teacher.Email);
        foreach (var seedStudent in teacher.Students)
        {
            var student = Created(
                Student.Create(user.Id, seedStudent.Name, null, seedStudent.Level, null), seedStudent.Name);
            await students.AddAsync(student, ct);
            await SeedPlansAsync(student.Id, seedStudent.Plans, today, ct);
        }
    }

    private async Task SeedPlansAsync(Guid studentId, SeedPlan[] plans, DateOnly today, CancellationToken ct)
    {
        foreach (var seedPlan in plans)
        {
            var plan = Created(
                LessonPlan.Create(studentId, seedPlan.Title, null, today.AddDays(seedPlan.DayOffset)), seedPlan.Title);
            plan.ChangeStatus(seedPlan.Status);
            await lessonPlans.AddAsync(plan, ct);
        }
    }

    private static T Created<T>(Result<T> result, string item) =>
        result.IsSuccess
            ? result.Value
            : throw new InvalidOperationException(
                $"Demo data item '{item}' could not be created: {string.Join(" ", result.Errors.Select(error => error.Message))} Expected valid seed data.");

    private sealed record SeedPlan(string Title, LessonPlanStatus Status, int DayOffset);

    private sealed record SeedStudent(string Name, EnglishLevel Level, SeedPlan[] Plans);

    private sealed record SeedTeacher(string Name, string Email, SeedStudent[] Students);
}
