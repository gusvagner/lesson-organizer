using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Api.Users;

namespace TaskManagement.Tests.Integration.Http;

public sealed class AuthenticationTests(SqlServerFixture sqlServer)
{
    private const string OtherSigningKey = "another-signing-key-of-at-least-32-bytes-9876543210";

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetTasks_ValidTokenOfExistingUser_Returns200()
    {
        await using var api = await TwoUserApi.StartAsync(sqlServer, Cancellation);

        using var response = await api.UserA.GetAsync("/api/tasks", Cancellation);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetTasks_WithoutToken_Returns401Problem()
    {
        await using var api = await TwoUserApi.StartAsync(sqlServer, Cancellation);
        using var anonymous = api.Factory.CreateClient();

        using var response = await anonymous.GetAsync("/api/tasks", Cancellation);

        await ApiJson.ReadProblemAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTasks_RejectedToken_ChallengeCarriesNoErrorDescription()
    {
        await using var api = await TwoUserApi.StartAsync(sqlServer, Cancellation);
        using var client = api.Factory.CreateClientWithToken(TestTokens.Expired(api.UserAId));

        using var response = await client.GetAsync("/api/tasks", Cancellation);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var challenge = string.Join(" ", response.Headers.WwwAuthenticate.Select(header => header.ToString()));
        Assert.StartsWith("Bearer", challenge);
        Assert.DoesNotContain("error_description", challenge);
        Assert.DoesNotContain("expired", challenge, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetTasks_ExpiredToken_Returns401()
    {
        await AssertRejectedAsync(api => TestTokens.Expired(api.UserAId));
    }

    [Fact]
    public async Task GetTasks_TokenNotValidYet_Returns401()
    {
        await AssertRejectedAsync(api => TestTokens.NotYetValid(api.UserAId));
    }

    [Fact]
    public async Task GetTasks_TokenWithoutExpiry_Returns401()
    {
        await AssertRejectedAsync(api => TestTokens.WithoutExpiry(api.UserAId));
    }

    [Fact]
    public async Task GetTasks_UserLookupFails_Returns500ProblemWithoutDetails()
    {
        await using var api = await TwoUserApi.StartAsync(
            sqlServer, Cancellation, services => services.AddScoped<IUserRepository, UnreachableUserRepository>());

        using var response = await api.UserA.GetAsync("/api/tasks", Cancellation);

        var problem = await ApiJson.ReadProblemAsync(response, HttpStatusCode.InternalServerError);
        Assert.DoesNotContain(UnreachableUserRepository.Detail, problem.GetRawText());
    }

    [Fact]
    public async Task GetTasks_TokenSignedWithAnotherKey_Returns401()
    {
        await AssertRejectedAsync(api => TestTokens.SignedWith(api.UserAId, OtherSigningKey));
    }

    [Fact]
    public async Task GetTasks_UnsignedToken_Returns401()
    {
        await AssertRejectedAsync(api => TestTokens.Unsigned(api.UserAId));
    }

    [Fact]
    public async Task GetTasks_TokenFromAnotherIssuer_Returns401()
    {
        await AssertRejectedAsync(api => TestTokens.IssuedBy(api.UserAId, "https://someone-else.example"));
    }

    [Fact]
    public async Task GetTasks_TokenForAnotherAudience_Returns401()
    {
        await AssertRejectedAsync(api => TestTokens.IssuedFor(api.UserAId, "some-other-api"));
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("12345")]
    [InlineData("")]
    public async Task GetTasks_SubjectThatIsNotAGuid_Returns401(string subject)
    {
        await AssertRejectedAsync(_ => TestTokens.WithSubject(subject));
    }

    [Fact]
    public async Task GetTasks_TokenWithoutSubject_Returns401()
    {
        await AssertRejectedAsync(_ => TestTokens.WithSubject(null));
    }

    [Fact]
    public async Task GetTasks_GuidSubjectWithoutUserRow_Returns401()
    {
        await AssertRejectedAsync(_ => TestTokens.For(Guid.NewGuid()));
    }

    [Fact]
    public async Task PostTask_GuidSubjectWithoutUserRow_Returns401AndStoresNothing()
    {
        await using var api = await TwoUserApi.StartAsync(sqlServer, Cancellation);
        using var unknownUser = api.Factory.CreateClientFor(Guid.NewGuid());

        using var response = await unknownUser.PostAsync("/api/tasks", ApiJson.Body("""{"title":"Ghost task"}"""), Cancellation);

        await ApiJson.ReadProblemAsync(response, HttpStatusCode.Unauthorized);
        await using var context = api.Database.CreateContext();
        Assert.False(await context.Tasks.AnyAsync(Cancellation));
    }

    private async Task AssertRejectedAsync(Func<TwoUserApi, string> createToken)
    {
        await using var api = await TwoUserApi.StartAsync(sqlServer, Cancellation);
        using var client = api.Factory.CreateClientWithToken(createToken(api));

        using var response = await client.GetAsync("/api/tasks", Cancellation);

        await ApiJson.ReadProblemAsync(response, HttpStatusCode.Unauthorized);
    }

    private sealed class UnreachableUserRepository : IUserRepository
    {
        public const string Detail = "Users table unreachable 4c1e";

        public Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new InvalidOperationException(Detail);
    }
}
