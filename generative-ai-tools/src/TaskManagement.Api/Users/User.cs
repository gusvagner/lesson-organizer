namespace TaskManagement.Api.Users;

public sealed class User(Guid id, string email, string name)
{
    public Guid Id { get; private set; } = id;

    public string Email { get; private set; } = email;

    public string Name { get; private set; } = name;
}
