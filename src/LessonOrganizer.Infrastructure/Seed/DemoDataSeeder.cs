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

    public async Task<bool> SeedAsync(CancellationToken ct)
    {
        if (await db.Users.AnyAsync(ct))
        {
            return false;
        }

        var today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        foreach (var teacher in DemoData.Teachers)
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
                Student.Create(user.Id, seedStudent.Name, seedStudent.Email, seedStudent.Level, seedStudent.Notes),
                seedStudent.Name);
            await students.AddAsync(student, ct);
            await SeedPlansAsync(student.Id, seedStudent.Plans, today, ct);
        }
    }

    private async Task SeedPlansAsync(Guid studentId, SeedPlan[] plans, DateOnly today, CancellationToken ct)
    {
        foreach (var seedPlan in plans)
        {
            var plan = Created(
                LessonPlan.Create(studentId, seedPlan.Title, seedPlan.Description, today.AddDays(seedPlan.DayOffset)),
                seedPlan.Title);
            plan.ChangeStatus(seedPlan.Status);
            await lessonPlans.AddAsync(plan, ct);
        }
    }

    private static T Created<T>(Result<T> result, string item) =>
        result.IsSuccess
            ? result.Value
            : throw new InvalidOperationException(
                $"Demo data item '{item}' could not be created: {string.Join(" ", result.Errors.Select(error => error.Message))} Expected valid seed data.");
}
