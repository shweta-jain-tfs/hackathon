namespace Pharos.Server.Parameters.Application.Parsers.ValueTypeParsers;

public class StringParsersTests
{
    private readonly StringParser _sut;

    public StringParsersTests()
    {
        _sut = new StringParser();
    }

    [Fact]
    public void Parse_ShouldReturnString()
    {
        var act = _sut.GetValue("5");
        act.Value.Should().Be("5");
    }

    [Fact]
    public void Parse_ShouldReturnEmpty_WhenEmpty()
    {
        var act = _sut.GetValue("");
        act.Value.Should().BeNull();
    }
}