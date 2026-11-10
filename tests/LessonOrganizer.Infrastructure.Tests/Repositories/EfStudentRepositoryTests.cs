using LessonOrganizer.Domain.LessonPlans;
using LessonOrganizer.Domain.Students;
using LessonOrganizer.Infrastructure.Repositories;
using LessonOrganizer.Infrastructure.Tests.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LessonOrganizer.Infrastructure.Tests.Repositories;

public class EfStudentRepositoryTests(SqlServerFixture sql)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Add_ThenNewContext_ReadsStudent()
    {
        await using var db = await TestDatabase.CreateAsync(sql);
        var teacherId = await db.AddTeacherAsync();
        var ana = await AddStudentAsync(db, teacherId, "Ana");
        await using var context = db.NewContext();
        var stored = await new EfStudentRepository(context).GetOwnedAsync(ana.Id, teacherId, Ct);
        Assert.Equal("Ana", stored?.Name);
    }

    [Fact]
    public async Task ListByTeacher_ReturnsOnlyOwnSortedByNameIgnoringCase()
    {
        await using var db = await TestDatabase.CreateAsync(sql);
        var teacherA = await db.AddTeacherAsync("a@example.com");
        var teacherB = await db.AddTeacherAsync("b@example.com");
        await AddStudentAsync(db, teacherA, "bruno");
        await AddStudentAsync(db, teacherA, "Ana");
        await AddStudentAsync(db, teacherA, "Carla");
        await AddStudentAsync(db, teacherB, "Aaron");
        var list = await ListAsync(db, teacherA, null);
        Assert.Equal(["Ana", "bruno", "Carla"], list.Select(student => student.Name));
    }

    [Fact]
    public async Task ListByTeacher_SearchIgnoresCase()
    {
        await using var db = await TestDatabase.CreateAsync(sql);
        var teacherId = await db.AddTeacherAsync();
        await AddStudentAsync(db, teacherId, "Ana");
        await AddStudentAsync(db, teacherId, "Mariana");
        await AddStudentAsync(db, teacherId, "Bruno");
        var list = await ListAsync(db, teacherId, "ANA");
        Assert.Equal(["Ana", "Mariana"], list.Select(student => student.Name));
    }

    [Fact]
    public async Task ListByTeacher_SearchWithWildcards_MatchesLiterally()
    {
        await using var db = await TestDatabase.CreateAsync(sql);
        var teacherId = await db.AddTeacherAsync();
        foreach (var name in new[] { "50% off", "5000 off", "a_b", "axb", "[x] mark", "x mark" })
        {
            await AddStudentAsync(db, teacherId, name);
        }

        Assert.Equal(["50% off"], (await ListAsync(db, teacherId, "50%")).Select(student => student.Name));
        Assert.Equal(["a_b"], (await ListAsync(db, teacherId, "a_b")).Select(student => student.Name));
        Assert.Equal(["[x] mark"], (await ListAsync(db, teacherId, "[x]")).Select(student => student.Name));
    }

    [Fact]
    public async Task GetOwned_OtherTeacher_ReturnsNull()
    {
        await using var db = await TestDatabase.CreateAsync(sql);
        var teacherA = await db.AddTeacherAsync("a@example.com");
        var teacherB = await db.AddTeacherAsync("b@example.com");
        var ana = await AddStudentAsync(db, teacherA, "Ana");
        await using var context = db.NewContext();
        Assert.Null(await new EfStudentRepository(context).GetOwnedAsync(ana.Id, teacherB, Ct));
    }

    [Fact]
    public async Task TryDelete_WithLessonPlans_ReturnsFalseAndKeepsStudent()
    {
        await using var db = await TestDatabase.CreateAsync(sql);
        var teacherId = await db.AddTeacherAsync();
        var ana = await AddStudentAsync(db, teacherId, "Ana");
        await using (var context = db.NewContext())
        {
            context.LessonPlans.Add(LessonPlan.Create(ana.Id, "Review", null, new DateOnly(2026, 11, 12)).Value);
            await context.SaveChangesAsync(Ct);
        }

        await using var deleting = db.NewContext();
        Assert.False(await new EfStudentRepository(deleting).TryDeleteAsync(ana.Id, teacherId, Ct));
        await using var reading = db.NewContext();
        Assert.Equal(1, await reading.Students.CountAsync(Ct));
    }

    [Fact]
    public async Task TryDelete_WithoutPlans_Removes()
    {
        await using var db = await TestDatabase.CreateAsync(sql);
        var teacherId = await db.AddTeacherAsync();
        var ana = await AddStudentAsync(db, teacherId, "Ana");
        await using var deleting = db.NewContext();
        Assert.True(await new EfStudentRepository(deleting).TryDeleteAsync(ana.Id, teacherId, Ct));
        await using var reading = db.NewContext();
        Assert.Equal(0, await reading.Students.CountAsync(Ct));
    }

    [Fact]
    public async Task TryDelete_OtherTeacher_ReturnsFalseAndKeepsStudent()
    {
        await using var db = await TestDatabase.CreateAsync(sql);
        var teacherA = await db.AddTeacherAsync("a@example.com");
        var teacherB = await db.AddTeacherAsync("b@example.com");
        var ana = await AddStudentAsync(db, teacherA, "Ana");
        await using var deleting = db.NewContext();
        Assert.False(await new EfStudentRepository(deleting).TryDeleteAsync(ana.Id, teacherB, Ct));
        await using var reading = db.NewContext();
        Assert.Equal(1, await reading.Students.CountAsync(Ct));
    }

    [Fact]
    public async Task Update_ThenNewContext_ReadsChanges()
    {
        await using var db = await TestDatabase.CreateAsync(sql);
        var teacherId = await db.AddTeacherAsync();
        var ana = await AddStudentAsync(db, teacherId, "Ana");
        ana.Update("Ana Maria", "ana@example.com", EnglishLevel.Advanced, "Notes");
        await using (var context = db.NewContext())
        {
            await new EfStudentRepository(context).UpdateAsync(ana, Ct);
        }

        await using var reading = db.NewContext();
        var stored = await new EfStudentRepository(reading).GetOwnedAsync(ana.Id, teacherId, Ct);
        Assert.Equal(("Ana Maria", "ana@example.com", EnglishLevel.Advanced, "Notes"),
            (stored!.Name, stored.Email, stored.EnglishLevel, stored.Notes));
    }

    [Fact]
    public async Task Update_MissingStudent_ThrowsNamingId()
    {
        await using var db = await TestDatabase.CreateAsync(sql);
        var ghost = Student.Create(await db.AddTeacherAsync(), "Ghost", null, EnglishLevel.Beginner, null).Value;
        await using var context = db.NewContext();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => new EfStudentRepository(context).UpdateAsync(ghost, Ct));
        Assert.Contains(ghost.Id.ToString(), ex.Message);
    }

    private static async Task<Student> AddStudentAsync(TestDatabase db, Guid teacherId, string name)
    {
        var student = Student.Create(teacherId, name, null, EnglishLevel.Intermediate, null).Value;
        await using var context = db.NewContext();
        await new EfStudentRepository(context).AddAsync(student, Ct);
        return student;
    }

    private static async Task<IReadOnlyList<Student>> ListAsync(TestDatabase db, Guid teacherId, string? search)
    {
        await using var context = db.NewContext();
        return await new EfStudentRepository(context).ListByTeacherAsync(teacherId, search, Ct);
    }
}
