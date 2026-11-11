using System.Net;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Api.Tasks;

namespace TaskManagement.Tests.Integration.Http;

public sealed class ErrorHandlingTests(SqlServerFixture sqlServer)
{
    private const string SensitiveDetail = "Sensitive internal detail 7f3a";

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ListTasks_UnhandledExceptionOutsideDevelopment_Returns500ProblemWithoutExceptionDetails()
    {
        await using var api = await TwoUserApi.StartAsync(
            sqlServer, Cancellation, services => services.AddScoped<ITaskRepository, ThrowingTaskRepository>());

        using var response = await api.UserA.GetAsync("/api/tasks", Cancellation);

        var problem = await ApiJson.ReadProblemAsync(response, HttpStatusCode.InternalServerError);
        var body = problem.GetRawText();
        Assert.DoesNotContain(SensitiveDetail, body);
        Assert.DoesNotContain(nameof(InvalidOperationException), body);
        Assert.DoesNotContain(" at ", body);
    }

    private sealed class ThrowingTaskRepository : ITaskRepository
    {
        public Task<TaskItem?> FindAsync(Guid ownerId, Guid taskId, CancellationToken cancellationToken) => throw Failure();

        public Task<TaskPage> ListAsync(Guid ownerId, TaskItemStatus? status, int page, int pageSize, CancellationToken cancellationToken) =>
            throw Failure();

        public Task AddAsync(TaskItem task, CancellationToken cancellationToken) => throw Failure();

        public Task<bool> UpdateAsync(Guid ownerId, TaskItem task, CancellationToken cancellationToken) => throw Failure();

        public Task<bool> DeleteAsync(Guid ownerId, Guid taskId, CancellationToken cancellationToken) => throw Failure();

        private static InvalidOperationException Failure() => new(SensitiveDetail);
    }
}
