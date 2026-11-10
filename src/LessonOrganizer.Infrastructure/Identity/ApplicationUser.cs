using Microsoft.AspNetCore.Identity;

namespace LessonOrganizer.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public const int NameMaxLength = 150;

    public string Name { get; set; } = "";
}
