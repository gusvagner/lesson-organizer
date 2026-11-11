using TaskManagement.Api.Tasks;

namespace TaskManagement.Tests.Unit;

public sealed class TaskValidatorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateCreate_MissingTitle_ReturnsTitleError(string? title)
    {
        var errors = TaskValidator.Validate(new CreateTaskRequest(title, null, null));

        Assert.Equal(["title"], errors.Keys);
    }

    [Fact]
    public void ValidateCreate_TitleOf200Characters_ReturnsNoErrors()
    {
        var errors = TaskValidator.Validate(new CreateTaskRequest(new string('t', 200), null, null));

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateCreate_TitleOf201Characters_ReturnsTitleErrorWithLength()
    {
        var errors = TaskValidator.Validate(new CreateTaskRequest(new string('t', 201), null, null));

        var message = Assert.Single(errors["title"]);
        Assert.Contains("200", message);
        Assert.Contains("201", message);
    }

    [Fact]
    public void ValidateCreate_TitleOf200CharactersWithSurroundingSpaces_ReturnsNoErrors()
    {
        var errors = TaskValidator.Validate(new CreateTaskRequest($"  {new string('t', 200)}  ", null, null));

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateCreate_DescriptionOf2000Characters_ReturnsNoErrors()
    {
        var errors = TaskValidator.Validate(new CreateTaskRequest("Title", new string('d', 2000), null));

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateCreate_DescriptionOf2001Characters_ReturnsDescriptionErrorWithLength()
    {
        var errors = TaskValidator.Validate(new CreateTaskRequest("Title", new string('d', 2001), null));

        var message = Assert.Single(errors["description"]);
        Assert.Contains("2000", message);
        Assert.Contains("2001", message);
    }

    [Fact]
    public void ValidateCreate_BlankDescriptionLongerThanLimit_ReturnsNoErrors()
    {
        var errors = TaskValidator.Validate(new CreateTaskRequest("Title", new string(' ', 3000), null));

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateCreate_MissingTitleAndLongDescription_ReturnsOneErrorPerField()
    {
        var errors = TaskValidator.Validate(new CreateTaskRequest(null, new string('d', 2001), null));

        Assert.Equal(["description", "title"], errors.Keys.Order());
    }

    [Fact]
    public void ValidateUpdate_CompleteRequest_ReturnsNoErrors()
    {
        var errors = TaskValidator.Validate(ValidUpdate());

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateUpdate_MissingStatus_ReturnsStatusError()
    {
        var errors = TaskValidator.Validate(ValidUpdate() with { Status = null });

        Assert.Equal(["status"], errors.Keys);
    }

    [Fact]
    public void ValidateUpdate_UndefinedStatusValue_ReturnsStatusErrorWithValue()
    {
        var errors = TaskValidator.Validate(ValidUpdate() with { Status = (TaskItemStatus)99 });

        var message = Assert.Single(errors["status"]);
        Assert.Contains("99", message);
    }

    [Fact]
    public void ValidateUpdate_TitleOf201Characters_ReturnsTitleError()
    {
        var errors = TaskValidator.Validate(ValidUpdate() with { Title = new string('t', 201) });

        Assert.Equal(["title"], errors.Keys);
    }

    [Fact]
    public void ValidateUpdate_DescriptionOf2001Characters_ReturnsDescriptionError()
    {
        var errors = TaskValidator.Validate(ValidUpdate() with { Description = new string('d', 2001) });

        Assert.Equal(["description"], errors.Keys);
    }

    [Fact]
    public void ValidateList_DefaultQuery_ReturnsNoErrors()
    {
        var errors = TaskValidator.Validate(new TaskListQuery());

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(5, 100)]
    public void ValidateList_PagingWithinLimits_ReturnsNoErrors(int page, int pageSize)
    {
        var errors = TaskValidator.Validate(new TaskListQuery { Page = page, PageSize = pageSize });

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ValidateList_PageBelowOne_ReturnsPageError(int page)
    {
        var errors = TaskValidator.Validate(new TaskListQuery { Page = page });

        Assert.Equal(["page"], errors.Keys);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void ValidateList_PageSizeOutsideOneToHundred_ReturnsPageSizeError(int pageSize)
    {
        var errors = TaskValidator.Validate(new TaskListQuery { PageSize = pageSize });

        Assert.Equal(["pageSize"], errors.Keys);
    }

    [Fact]
    public void ValidateList_PageWhoseOffsetOverflowsInt_ReturnsPageErrorWithValue()
    {
        var errors = TaskValidator.Validate(new TaskListQuery { Page = 21_474_838, PageSize = 100 });

        var message = Assert.Single(errors["page"]);
        Assert.Contains("21474838", message);
    }

    [Fact]
    public void ValidateList_PageWhoseOffsetIsExactlyIntMax_ReturnsNoErrors()
    {
        var errors = TaskValidator.Validate(new TaskListQuery { Page = int.MaxValue, PageSize = 1 });

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateList_UndefinedStatusValue_ReturnsStatusError()
    {
        var errors = TaskValidator.Validate(new TaskListQuery { Status = (TaskItemStatus)7 });

        Assert.Equal(["status"], errors.Keys);
    }

    private static UpdateTaskRequest ValidUpdate() =>
        new("Title", "Description", TaskItemStatus.InProgress, new DateOnly(2026, 12, 1));
}
