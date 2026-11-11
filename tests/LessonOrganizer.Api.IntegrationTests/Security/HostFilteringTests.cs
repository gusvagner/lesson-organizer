using System.Net;
using LessonOrganizer.Testing.SqlServer;

namespace LessonOrganizer.Api.IntegrationTests.Security;

public sealed class HostFilteringTests(SqlServerFixture sql)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task AllowedHostsLocalhost_RequestForAnotherHost_Returns400()
    {
        await using var factory = new ApiFactory(sql, new Dictionary<string, string?> { ["AllowedHosts"] = "localhost;127.0.0.1" });
        var http = factory.CreateClient();
        using var rebound = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/auth/csrf");
        rebound.Headers.Host = "attacker.example";
        using var local = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/auth/csrf");

        Assert.Equal(HttpStatusCode.BadRequest, (await http.SendAsync(rebound, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await http.SendAsync(local, Ct)).StatusCode);
    }
}
