namespace TaskManagement.Api.Http;

public static class EnumNames
{
    public static string? Match(Type enumType, string? text) =>
        Enum.GetNames(enumType).FirstOrDefault(name => string.Equals(name, text, StringComparison.OrdinalIgnoreCase));

    public static string Allowed(Type enumType) => string.Join(", ", Enum.GetNames(enumType));
}
