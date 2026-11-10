using LessonOrganizer.Testing.SqlServer;
using LessonOrganizer.Application.LessonPlans;
using LessonOrganizer.Domain.LessonPlans;
using LessonOrganizer.Domain.Students;
using LessonOrganizer.Infrastructure.Repositories;
using LessonOrganizer.Infrastructure.Tests.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LessonOrganizer.Infrastructure.Tests.Repositories;

public class EfLessonPlanRepositoryTests(SqlServerFixture sql)
{
    private static readonly DateOnly Today = new(2026, 11, 12);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ListByTeacher_SortsByDateThenTitleThenId()
    {
        await using var db = await TestDatabase.CreateAsync(sql);
        var teacherId = await db.AddTeacherAsync();
        var ana = await AddStudentAsync(db, teacherId, "Ana");
        await AddPlanAsync(db, ana.Id, "b later", Today.AddDays(2));
        await AddPlanAsync(db, ana.Id, "B today", Today);
        await AddPlanAsync(db, ana.Id, "a today", Today);
        var list = await ListAsync(db, teacherId, new LessonPlanFilter(null, null));
        Assert.Equal(["a today", "B today", "b later"], list.Select(plan => plan.Title));
    }

    [Fact]
    public async Task ListByTeacher_FiltersCombine()
    {
        await using var db = await TestDatabase.CreateAsync(sql);
        var teacherA = await db.AddTeacherAsync("a@example.com");
        var teacherB = await db.AddTeacherAsync("b@example.com");
        var ana = await AddStudentAsync(db, teacherA, "Ana");
        var bruno = await AddStudentAsync(db, teacherA, "Bruno");
        var bob = await AddStudentAsync(db, teacherB, "Bob");
        await AddPlanAsync(db, ana.Id, "Ana ready", Today, LessonPlanStatus.Ready);
        await AddPlanAsync(db, ana.Id, "Ana to prepare", Today);
        await AddPlanAsync(db, bruno.Id, "Bruno ready", Today, LessonPlanStatus.Ready);
        await AddPlanAsync(db, bob.Id, "Bob ready", Today, LessonPlanStatus.Ready);
        var byStudentAndStatus = await ListAsync(db, teacherA, new LessonPlanFilter(ana.Id, LessonPlanStatus.Ready));
        var byStatus = await ListAsync(db, teacherA, new LessonPlanFilter(null, LessonPlanStatus.Ready));
        Assert.Equal(["Ana ready"], byStudentAndStatus.Select(plan => plan.Title));
        Assert.Equal(["Ana ready", "Bruno ready"], byStatus.Select(plan => plan.Title));
    }

    [Fact]
    public async Task GetOwned_ThroughOtherTeachersStudent_ReturnsNull()
    {
        await using var db = await TestDatabase.CreateAsync(sql);
        var teacherA = await db.AddTeacherAsync("a@example.com");
        var teacherB = await db.AddTeacherAsync("b@example.com");
        var ana = await AddStudentAsync(db, teacherA, "Ana");
        var plan = await AddPlanAsync(db, ana.Id, "Review", Today);
        await using var context = db.NewContext();
        var repository = new EfLessonPlanRepository(context);
        Assert.Null(await repository.GetOwnedAsync(plan.Id, teacherB, Ct));
        Assert.NotNull(await repository.GetOwnedAsync(plan.Id, teacherA, Ct));
    }

    [Fact]
    public async Task Add_StudentMissing_ThrowsNamingIds()
    {
        await using var db = await TestDatabase.CreateAsync(sql);
        var orphan = LessonPlan.Create(Guid.CreateVersion7(), "Orphan", null, Today).Value;
        await using var context = db.NewContext();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => new EfLessonPlanRepository(context).AddAsync(orphan, Ct));
        Assert.Equal(
            $"Lesson plan {orphan.Id} references student {orphan.StudentId}, which does not exist. Expected an existing student id.",
            ex.Message);
    }

    [Fact]
    public async Task CountByStudents_ReturnsZeroForStudentsWithoutPlans()
    {
        await using var db = await TestDatabase.CreateAsync(sql);
        var teacherId = await db.AddTeacherAsync();
        var ana = await AddStudentAsync(db, teacherId, "Ana");
        var bruno = await AddStudentAsync(db, teacherId, "Bruno");
        await AddPlanAsync(db, ana.Id, "One", Today);
        await AddPlanAsync(db, ana.Id, "Two", Today);
        await using var context = db.NewContext();
        var counts = await new EfLessonPlanRepository(context).CountByStudentsAsync([ana.Id, bruno.Id], Ct);
        Assert.Equal(2, counts[ana.Id]);
        Assert.Equal(0, counts[bruno.Id]);
    }

    [Fact]
    public async Task Update_ThenNewContext_ReadsChanges()
    {
        await using var db = await TestDatabase.CreateAsync(sql);
        var teacherId = await db.AddTeacherAsync();
        var ana = await AddStudentAsync(db, teacherId, "Ana");
        var bruno = await AddStudentAsync(db, teacherId, "Bruno");
        var plan = await AddPlanAsync(db, ana.Id, "Review", Today);
        plan.Update(bruno.Id, "Past simple", "Stories", Today.AddDays(1));
        plan.ChangeStatus(LessonPlanStatus.Delivered);
        await using (var context = db.NewContext())
        {
            await new EfLessonPlanRepository(context).UpdateAsync(plan, Ct);
        }

        await using var reading = db.NewContext();
        var stored = await reading.LessonPlans.SingleAsync(Ct);
        Assert.Equal((bruno.Id, "Past simple", "Stories", Today.AddDays(1), LessonPlanStatus.Delivered),
            (stored.StudentId, stored.Title, stored.Description, stored.LessonDate, stored.Status));
    }

    [Fact]
    public async Task Delete_Removes()
    {
        await using var db = await TestDatabase.CreateAsync(sql);
        var teacherId = await db.AddTeacherAsync();
        var ana = await AddStudentAsync(db, teacherId, "Ana");
        var plan = await AddPlanAsync(db, ana.Id, "Review", Today);
        await using (var context = db.NewContext())
        {
            await new EfLessonPlanRepository(context).DeleteAsync(plan.Id, Ct);
        }

        await using var reading = db.NewContext();
        Assert.Equal(0, await reading.LessonPlans.CountAsync(Ct));
    }

    private static async Task<Student> AddStudentAsync(TestDatabase db, Guid teacherId, string name)
    {
        var student = Student.Create(teacherId, name, null, EnglishLevel.Intermediate, null).Value;
        await using var context = db.NewContext();
        await new EfStudentRepository(context).AddAsync(student, Ct);
        return student;
    }

    private static async Task<LessonPlan> AddPlanAsync(
        TestDatabase db, Guid studentId, string title, DateOnly date, LessonPlanStatus status = LessonPlanStatus.ToPrepare)
    {
        var plan = LessonPlan.Create(studentId, title, null, date).Value;
        plan.ChangeStatus(status);
        await using var context = db.NewContext();
        await new EfLessonPlanRepository(context).AddAsync(plan, Ct);
        return plan;
    }

    private static async Task<IReadOnlyList<LessonPlan>> ListAsync(TestDatabase db, Guid teacherId, LessonPlanFilter filter)
    {
        await using var context = db.NewContext();
        return await new EfLessonPlanRepository(context).ListByTeacherAsync(teacherId, filter, Ct);
    }
}
