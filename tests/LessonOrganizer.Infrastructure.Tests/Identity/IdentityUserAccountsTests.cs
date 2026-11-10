using LessonOrganizer.Application.Abstractions;
using LessonOrganizer.Application.Auth;
using LessonOrganizer.Domain.Common;
using LessonOrganizer.Infrastructure.Tests.Persistence;
using LessonOrganizer.Testing.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LessonOrganizer.Infrastructure.Tests.Identity;

public sealed class IdentityUserAccountsTests(SqlServerFixture sql) : IAsyncLifetime
{
    private const string Password = "Demo1234!";

    private TestDatabase _db = null!;
    private ServiceProvider _services = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _db = await TestDatabase.CreateAsync(sql);
        _services = new ServiceCollection().AddLogging().AddInfrastructure(_db.ConnectionString).BuildServiceProvider();
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _db.DisposeAsync();
    }

    [Fact]
    public async Task CheckPassword_FiveWrongThenRight_ReturnsLockedOut()
    {
        await CreateAsync("maria@example.com");
        for (var i = 0; i < 5; i++)
        {
            await CheckAsync("maria@example.com", "wrong-pass");
        }

        Assert.Equal(LoginStatus.LockedOut, (await CheckAsync("maria@example.com", Password)).Status);
    }

    [Fact]
    public async Task Create_EmailWithApostrophe_Succeeds() =>
        Assert.True((await CreateAsync("o'neil@example.com")).IsSuccess);

    [Fact]
    public async Task Create_PlusAddress_Succeeds() =>
        Assert.True((await CreateAsync("ana+work@example.com")).IsSuccess);

    [Fact]
    public async Task Create_ThenCheckPassword_Succeeds()
    {
        await CreateAsync("maria@example.com");
        var outcome = await CheckAsync("maria@example.com", Password);
        Assert.Equal(LoginStatus.Succeeded, outcome.Status);
        Assert.Equal(("Maria", "maria@example.com"), (outcome.User!.Name, outcome.User.Email));
    }

    [Fact]
    public async Task Check_EmailInOtherCase_Succeeds()
    {
        await CreateAsync("maria@example.com");
        Assert.Equal(LoginStatus.Succeeded, (await CheckAsync("MARIA@EXAMPLE.COM", Password)).Status);
    }

    [Fact]
    public async Task Create_SameEmailOtherCase_ReturnsConflict()
    {
        await CreateAsync("maria@example.com");
        var result = await CreateAsync("Maria@Example.com");
        Assert.Equal(Error.Conflict("An account with this email already exists."), Assert.Single(result.Errors));
    }

    [Fact]
    public async Task Create_StoresHashNotPassword()
    {
        await CreateAsync("maria@example.com");
        await using var context = _db.NewContext();
        var hash = await context.Users.Select(user => user.PasswordHash).SingleAsync(Ct);
        Assert.False(string.IsNullOrEmpty(hash));
        Assert.DoesNotContain(Password, hash, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CheckPassword_UnknownEmail_ReturnsInvalidCredentials() =>
        Assert.Equal(LoginStatus.InvalidCredentials, (await CheckAsync("nobody@example.com", Password)).Status);

    [Fact]
    public async Task CheckPassword_FourWrongThenRight_ResetsCount()
    {
        await CreateAsync("maria@example.com");
        for (var i = 0; i < 4; i++)
        {
            await CheckAsync("maria@example.com", "wrong-pass");
        }

        Assert.Equal(LoginStatus.Succeeded, (await CheckAsync("maria@example.com", Password)).Status);
        for (var i = 0; i < 4; i++)
        {
            Assert.Equal(LoginStatus.InvalidCredentials, (await CheckAsync("maria@example.com", "wrong-pass")).Status);
        }

        Assert.Equal(LoginStatus.Succeeded, (await CheckAsync("maria@example.com", Password)).Status);
    }

    [Fact]
    public async Task FindById_ReturnsNameAndEmail()
    {
        var created = await CreateAsync("maria@example.com");
        await using var scope = _services.CreateAsyncScope();
        var found = await scope.ServiceProvider.GetRequiredService<IUserAccounts>().FindByIdAsync(created.Value.Id, Ct);
        Assert.Equal(created.Value, found);
    }

    [Fact]
    public async Task FindById_Missing_ReturnsNull()
    {
        await using var scope = _services.CreateAsyncScope();
        Assert.Null(await scope.ServiceProvider.GetRequiredService<IUserAccounts>().FindByIdAsync(Guid.CreateVersion7(), Ct));
    }

    private async Task<Result<UserResponse>> CreateAsync(string email)
    {
        await using var scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IUserAccounts>().CreateAsync("Maria", email, Password, Ct);
    }

    private async Task<LoginOutcome> CheckAsync(string email, string password)
    {
        await using var scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IUserAccounts>().CheckPasswordAsync(email, password, Ct);
    }
}
