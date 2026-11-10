using System.Text;

namespace LessonOrganizer.Infrastructure.Repositories;

public static class LikePattern
{
    public const string EscapeCharacter = "\\";

    public static string Contains(string text)
    {
        var pattern = new StringBuilder("%", text.Length + 2);
        foreach (var character in text)
        {
            if (character is '\\' or '%' or '_' or '[')
            {
                pattern.Append('\\');
            }

            pattern.Append(character);
        }

        return pattern.Append('%').ToString();
    }
}
