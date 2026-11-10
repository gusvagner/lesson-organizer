using System.Net.Mail;

namespace LessonOrganizer.Domain.Common;

public static class EmailFormat
{
    public static bool IsValid(string email) =>
        MailAddress.TryCreate(email, out var parsed) && parsed.Address == email;
}
