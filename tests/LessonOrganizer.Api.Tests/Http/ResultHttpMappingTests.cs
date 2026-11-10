using LessonOrganizer.Api.Http;
using LessonOrganizer.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace LessonOrganizer.Api.Tests.Http;

public class ResultHttpMappingTests
{
    [Fact]
    public void Failure_ValidationErrors_Returns400WithFieldKeys()
    {
        var result = Result<int>.Failure([Error.Validation("name", "Name is required."), Error.Validation("email", "Email is not a valid email address.")]);
        var problem = AssertProblem<ValidationProblemDetails>(ResultHttpMapping.ToOk(result), 400);
        Assert.Equal(["Name is required."], problem.Errors["name"]);
        Assert.Equal(["Email is not a valid email address."], problem.Errors["email"]);
    }

    [Fact]
    public void Failure_NotFound_Returns404ProblemWithDetail()
    {
        var problem = AssertProblem<ProblemDetails>(ResultHttpMapping.ToNoContent(Result.Failure(Error.NotFound("Student not found."))), 404);
        Assert.Equal(("Not Found", "Student not found."), (problem.Title, problem.Detail));
    }

    [Fact]
    public void Failure_Conflict_Returns409() =>
        AssertProblem<ProblemDetails>(ResultHttpMapping.ToNoContent(Result.Failure(Error.Conflict("Has plans."))), 409);

    [Fact]
    public void Failure_Unauthorized_Returns401() =>
        AssertProblem<ProblemDetails>(ResultHttpMapping.ToOk(Result<int>.Failure(Error.Unauthorized("Invalid email or password."))), 401);

    [Fact]
    public void ValidationWinsOverNotFound_Returns400() =>
        AssertProblem<ValidationProblemDetails>(
            ResultHttpMapping.ToNoContent(Result.Failure([Error.NotFound("Missing."), Error.Validation("title", "Title is required.")])),
            400);

    [Fact]
    public void ToOk_Success_Returns200WithValue() =>
        Assert.Equal(42, Assert.IsType<OkObjectResult>(ResultHttpMapping.ToOk(Result<int>.Success(42))).Value);

    [Fact]
    public void ToCreated_Success_Returns201WithRouteValues()
    {
        var id = Guid.CreateVersion7();
        var created = Assert.IsType<CreatedAtActionResult>(ResultHttpMapping.ToCreated(Result<Guid>.Success(id), "Get", value => new { id = value }));
        Assert.Equal(("Get", (object?)id, (object?)id), (created.ActionName, created.RouteValues!["id"], created.Value));
    }

    [Fact]
    public void ToNoContent_Success_Returns204() =>
        Assert.IsType<NoContentResult>(ResultHttpMapping.ToNoContent(Result.Success()));

    private static T AssertProblem<T>(IActionResult actionResult, int status)
        where T : ProblemDetails
    {
        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(status, objectResult.StatusCode);
        var problem = Assert.IsType<T>(objectResult.Value);
        Assert.Equal(status, problem.Status);
        return problem;
    }
}
