using LessonOrganizer.Domain.Common;

namespace LessonOrganizer.Domain.Tests.Common;

public class ResultTests
{
    [Fact]
    public void Success_HasNoErrors()
    {
        var result = Result.Success();
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Value_OnFailure_ThrowsWithErrorMessages()
    {
        var result = Result<int>.Failure(Error.Validation("name", "Name is required."));
        var ex = Assert.Throws<InvalidOperationException>(() => result.Value);
        Assert.Contains("Name is required.", ex.Message);
    }

    [Fact]
    public void Failure_WithError_IsNotSuccess()
    {
        var result = Result.Failure(Error.NotFound("Student not found."));
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.NotFound, Assert.Single(result.Errors).Kind);
    }

    [Fact]
    public void ValueSuccess_ReturnsValue() => Assert.Equal(42, Result<int>.Success(42).Value);

    [Fact]
    public void Unauthorized_HasKindUnauthorized() =>
        Assert.Equal(new Error(ErrorKind.Unauthorized, "Invalid email or password."), Error.Unauthorized("Invalid email or password."));
}
