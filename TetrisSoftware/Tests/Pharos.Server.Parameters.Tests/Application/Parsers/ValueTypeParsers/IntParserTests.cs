namespace Pharos.Server.Parameters.Application.Parsers.ValueTypeParsers;

public class IntParserTests
{
    private readonly IntParser _sut;

    public IntParserTests()
    {
        _sut = new IntParser();
    }

    [Fact]
    public void Parse_ShouldReturnInt()
    {
        var act = _sut.GetValue("5");
        act.Value.Should().Be(5);
    }

    [Fact]
    public void Parse_ShouldReturnDefault_WhenEmpty()
    {
        var act = _sut.GetValue("");
        act.Value.Should().Be(0);
    }

    [Fact]
    public void Parse_ShouldFail_WhenFailsToParse()
    {
        Assert.Throws<FormatException>(() => _sut.GetValue("1.1"));
    }
}