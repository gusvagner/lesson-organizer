using LessonOrganizer.Domain.Common;

namespace LessonOrganizer.Domain.Tests.Common;

public class TextFieldTests
{
    [Fact]
    public void Required_ValueWithSpacesAround150Chars_ReturnsTrimmedValue()
    {
        var errors = new List<Error>();
        var value = TextField.Required("  " + new string('a', 150) + "  ", "name", "Name", 150, errors);
        Assert.Equal(new string('a', 150), value);
        Assert.Empty(errors);
    }

    [Fact]
    public void Required_151CharsAfterTrim_AddsMaxLengthError()
    {
        var errors = new List<Error>();
        TextField.Required(new string('a', 151), "name", "Name", 150, errors);
        Assert.Equal(Error.Validation("name", "Name must be at most 150 characters."), Assert.Single(errors));
    }

    [Fact]
    public void Required_WhitespaceOnly_AddsRequiredError()
    {
        var errors = new List<Error>();
        var value = TextField.Required(" \t\n ", "name", "Name", 150, errors);
        Assert.Null(value);
        Assert.Equal(Error.Validation("name", "Name is required."), Assert.Single(errors));
    }

    [Fact]
    public void Required_Null_AddsRequiredError()
    {
        var errors = new List<Error>();
        TextField.Required(null, "name", "Name", 150, errors);
        Assert.Equal(Error.Validation("name", "Name is required."), Assert.Single(errors));
    }

    [Fact]
    public void Optional_Whitespace_ReturnsNull()
    {
        var errors = new List<Error>();
        var value = TextField.Optional("   ", "notes", "Notes", 2000, errors);
        Assert.Null(value);
        Assert.Empty(errors);
    }

    [Fact]
    public void Optional_TooLong_AddsMaxLengthError()
    {
        var errors = new List<Error>();
        TextField.Optional(new string('a', 2001), "notes", "Notes", 2000, errors);
        Assert.Equal(Error.Validation("notes", "Notes must be at most 2000 characters."), Assert.Single(errors));
    }
}
