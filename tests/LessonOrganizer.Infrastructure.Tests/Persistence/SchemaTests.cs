using LessonOrganizer.Domain.LessonPlans;
using LessonOrganizer.Domain.Students;
using LessonOrganizer.Infrastructure.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LessonOrganizer.Infrastructure.Tests.Persistence;

public class SchemaTests(SqlServerFixture sql)
{
    private static readonly DateOnly Today = new(2026, 11, 12);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task SaveLessonPlan_StudentMissing_ViolatesForeignKey()
    {
        await using var db = await TestDatabase.CreateAsync(sql);
        await using var context = db.NewContext();
        context.LessonPlans.Add(LessonPlan.Create(Guid.CreateVersion7(), "Orphan", null, Today).Value);
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(Ct));
        Assert.Equal(547, Assert.IsType<SqlException>(ex.InnerException).Number);
    }

    [Fact]
    public async Task Model_MatchesLatestMigration()
    {
        await using var db = await TestDatabase.CreateAsync(sql);
        await using var context = db.NewContext();
        Assert.False(context.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task Migrate_EmptyDatabase_CreatesUserStudentAndLessonPlanTables()
    {
        await using var db = await TestDatabase.CreateAsync(sql);
        await using var context = db.NewContext();
        var tables = await context.Database
            .SqlQueryRaw<string>("SELECT TABLE_NAME AS Value FROM INFORMATION_SCHEMA.TABLES")
            .ToListAsync(Ct);
        Assert.Contains("AspNetUsers", tables);
        Assert.Contains("Students", tables);
        Assert.Contains("LessonPlans", tables);
        Assert.DoesNotContain("AspNetRoles", tables);
    }

    [Fact]
    public async Task DeleteStudent_WithLessonPlans_ViolatesForeignKey()
    {
        await using var db = await TestDatabase.CreateAsync(sql);
        var student = await AddStudentAsync(db, await db.AddTeacherAsync());
        await using (var context = db.NewContext())
        {
            context.LessonPlans.Add(LessonPlan.Create(student.Id, "Review", null, Today).Value);
            await context.SaveChangesAsync(Ct);
        }

        await using var deleting = db.NewContext();
        deleting.Students.Remove(await deleting.Students.SingleAsync(Ct));
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => deleting.SaveChangesAsync(Ct));
        Assert.Equal(547, Assert.IsType<SqlException>(ex.InnerException).Number);
    }

    [Fact]
    public async Task SaveUsers_SameNormalizedUserName_ViolatesUniqueIndex()
    {
        await using var db = await TestDatabase.CreateAsync(sql);
        await db.AddTeacherAsync("maria@example.com");
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.AddTeacherAsync("Maria@Example.com"));
        Assert.Equal(2601, Assert.IsType<SqlException>(ex.InnerException).Number);
    }

    [Fact]
    public async Task SaveStudent_StoresEnglishLevelAsName()
    {
        await using var db = await TestDatabase.CreateAsync(sql);
        await AddStudentAsync(db, await db.AddTeacherAsync());
        await using var context = db.NewContext();
        var level = await context.Database.SqlQueryRaw<string>("SELECT EnglishLevel AS Value FROM Students").SingleAsync(Ct);
        Assert.Equal("Intermediate", level);
    }

    [Fact]
    public async Task SaveStudent_ThenNewContext_ReadsSameValues()
    {
        await using var db = await TestDatabase.CreateAsync(sql);
        var teacherId = await db.AddTeacherAsync();
        var student = Student.Create(teacherId, "Ana", "ana@example.com", EnglishLevel.UpperIntermediate, "Likes music").Value;
        var plan = LessonPlan.Create(student.Id, "Present perfect", "Conversation", Today).Value;
        plan.ChangeStatus(LessonPlanStatus.Delivered);
        await using (var context = db.NewContext())
        {
            context.Students.Add(student);
            context.LessonPlans.Add(plan);
            await context.SaveChangesAsync(Ct);
        }

        await using var reading = db.NewContext();
        var storedStudent = await reading.Students.SingleAsync(Ct);
        var storedPlan = await reading.LessonPlans.SingleAsync(Ct);
        Assert.Equal((student.Id, teacherId, "Ana", "ana@example.com", EnglishLevel.UpperIntermediate, "Likes music"),
            (storedStudent.Id, storedStudent.TeacherId, storedStudent.Name, storedStudent.Email, storedStudent.EnglishLevel, storedStudent.Notes));
        Assert.Equal((plan.Id, student.Id, "Present perfect", "Conversation", Today, LessonPlanStatus.Delivered),
            (storedPlan.Id, storedPlan.StudentId, storedPlan.Title, storedPlan.Description, storedPlan.LessonDate, storedPlan.Status));
    }

    [Fact]
    public async Task SaveStudent_TeacherMissing_ViolatesForeignKey()
    {
        await using var db = await TestDatabase.CreateAsync(sql);
        await using var context = db.NewContext();
        context.Students.Add(Student.Create(Guid.CreateVersion7(), "Ana", null, EnglishLevel.Beginner, null).Value);
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(Ct));
        Assert.Equal(547, Assert.IsType<SqlException>(ex.InnerException).Number);
    }

    private static async Task<Student> AddStudentAsync(TestDatabase db, Guid teacherId)
    {
        var student = Student.Create(teacherId, "Ana", null, EnglishLevel.Intermediate, null).Value;
        await using var context = db.NewContext();
        context.Students.Add(student);
        await context.SaveChangesAsync(Ct);
        return student;
    }
}
