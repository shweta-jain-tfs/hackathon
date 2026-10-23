#pragma warning disable CS8625

namespace Pharos.Server.Parameters.Application.Parsers.ValueTypeParsers;

public class DateParserTests
{
    private readonly DateParser _sut;

    public DateParserTests()
    {
        _sut = new DateParser();
    }

    [Fact]
    public void Parse_WithNull_ShouldNotThrowException()
    {
        var act = () => _sut.GetValue(null);
        act.Should().NotThrow<Exception>();
    }

    [Theory]
    [InlineData("790119:123456", 2079, 01, 19, 12, 34, 56)]
    [InlineData("800119:123456", 1980, 01, 19, 12, 34, 56)]
    [InlineData("820119:123456", 1982, 01, 19, 12, 34, 56)]
    [InlineData("980709:131532", 1998, 07, 09, 13, 15, 32)]
    [InlineData("041224:150000", 2004, 12, 24, 15, 00, 00)]
    public void Parse_ValidDate_ShouldReturnCorrectValues(string inputDateTime, int year, int month, int day, int hour, int minute, int second)
    {
        var dateTime = _sut.GetValue(inputDateTime);

        var expectedDateTime = new DateTime(year, month, day, hour, minute, second);

        dateTime.Value.Should().Be(expectedDateTime);
    }
}