using Microsoft.Extensions.DependencyInjection;

namespace TaskManagement.Tests.Integration.Http;

public sealed class TwoUserApi : IAsyncDisposable
{
    private TwoUserApi(TestDatabase database, ApiFactory factory, Guid userAId, Guid userBId)
    {
        Database = database;
        Factory = factory;
        UserAId = userAId;
        UserBId = userBId;
        UserA = factory.CreateClientFor(userAId);
        UserB = factory.CreateClientFor(userBId);
    }

    public TestDatabase Database { get; }

    public ApiFactory Factory { get; }

    public Guid UserAId { get; }

    public Guid UserBId { get; }

    public HttpClient UserA { get; }

    public HttpClient UserB { get; }

    public static async Task<TwoUserApi> StartAsync(
        SqlServerFixture sqlServer, CancellationToken cancellationToken, Action<IServiceCollection>? replaceServices = null)
    {
        var database = await sqlServer.CreateMigratedDatabaseAsync(cancellationToken);
        var userA = await database.SeedUserAsync(cancellationToken);
        var userB = await database.SeedUserAsync(cancellationToken);
        return new TwoUserApi(database, ApiFactory.For(database, replaceServices), userA.Id, userB.Id);
    }

    public async ValueTask DisposeAsync()
    {
        UserA.Dispose();
        UserB.Dispose();
        await Factory.DisposeAsync();
    }
}
