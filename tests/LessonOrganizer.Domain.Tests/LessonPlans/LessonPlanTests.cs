using LessonOrganizer.Domain.Common;
using LessonOrganizer.Domain.LessonPlans;

namespace LessonOrganizer.Domain.Tests.LessonPlans;

public class LessonPlanTests
{
    private static readonly Guid StudentId = Guid.CreateVersion7();
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);

    private static LessonPlan Plan() => LessonPlan.Create(StudentId, "Present perfect", null, Today).Value;

    [Fact]
    public void Create_ValidData_StartsAsToPrepare()
    {
        var plan = Plan();
        Assert.Equal(LessonPlanStatus.ToPrepare, plan.Status);
        Assert.Equal(StudentId, plan.StudentId);
        Assert.Equal(7, plan.Id.Version);
    }

    [Fact]
    public void Create_PastDate_IsAccepted() =>
        Assert.True(LessonPlan.Create(StudentId, "Review", null, Today.AddDays(-30)).IsSuccess);

    [Fact]
    public void Create_TitleWhitespaceOnly_ReturnsTitleRequired()
    {
        var result = LessonPlan.Create(StudentId, "   ", null, Today);
        Assert.Equal(Error.Validation("title", "Title is required."), Assert.Single(result.Errors));
    }

    [Fact]
    public void Create_DescriptionOver5000_ReturnsDescriptionError()
    {
        var result = LessonPlan.Create(StudentId, "Review", new string('d', 5001), Today);
        Assert.Equal(Error.Validation("description", "Description must be at most 5000 characters."), Assert.Single(result.Errors));
    }

    [Fact]
    public void ChangeStatus_DeliveredBackToToPrepare_Succeeds()
    {
        var plan = Plan();
        plan.ChangeStatus(LessonPlanStatus.Delivered);
        var result = plan.ChangeStatus(LessonPlanStatus.ToPrepare);
        Assert.True(result.IsSuccess);
        Assert.Equal(LessonPlanStatus.ToPrepare, plan.Status);
    }

    [Fact]
    public void ChangeStatus_UndefinedValue_ReturnsStatusErrorAndKeepsStatus()
    {
        var plan = Plan();
        var result = plan.ChangeStatus((LessonPlanStatus)7);
        Assert.Equal(Error.Validation("status", "Status must be one of: ToPrepare, Ready, Delivered."), Assert.Single(result.Errors));
        Assert.Equal(LessonPlanStatus.ToPrepare, plan.Status);
    }

    [Fact]
    public void Update_TitleTooLong_LeavesPlanUnchanged()
    {
        var plan = Plan();
        var result = plan.Update(Guid.CreateVersion7(), new string('t', 151), "New description", Today.AddDays(3));
        Assert.False(result.IsSuccess);
        Assert.Equal("Present perfect", plan.Title);
        Assert.Equal(StudentId, plan.StudentId);
        Assert.Null(plan.Description);
        Assert.Equal(Today, plan.LessonDate);
    }

    [Fact]
    public void Update_ValidData_KeepsStatus()
    {
        var plan = Plan();
        plan.ChangeStatus(LessonPlanStatus.Ready);
        var otherStudent = Guid.CreateVersion7();
        var result = plan.Update(otherStudent, " Past simple ", "Stories", Today.AddDays(1));
        Assert.True(result.IsSuccess);
        Assert.Equal(LessonPlanStatus.Ready, plan.Status);
        Assert.Equal("Past simple", plan.Title);
        Assert.Equal(otherStudent, plan.StudentId);
    }
}
