namespace Pharos.Server.Parameters.Application.Parsers.ValueTypeParsers;

public class DoubleParserTests
{
    private readonly DoubleParser _sut;

    public DoubleParserTests()
    {
        const int maxDecimals = 3;
        _sut = new DoubleParser(maxDecimals);
    }

    [Fact]
    public void Parse_ShouldReturnDouble()
    {
        var act = _sut.GetValue("5.123");
        act.Value.Should().Be(5.123);
    }
    [Fact]
    public void Parse_ShouldReturnDouble_WithComma()
    {
        var act = _sut.GetValue("5,123");
        act.Value.Should().Be(5.123);
    }
    [Fact]
    public void Parse_ShouldReturnDefault()
    {
        var act = _sut.GetValue("5..123");
        act.Value.Should().Be(default);
    }
    [Fact]
    public void Parse_ShouldReturnDouble2()
    {
        var act = _sut.GetValue("5.1");
        act.Value.Should().Be(5.100);
    }
    [Fact]
    public void Parse_ShouldReturnDouble3()
    {
        var act = _sut.GetValue("5.1");
        act.Value.Should().Be(5.1);
    }

    [Fact]
    public void Parse_ShouldRound_WhenTooManyDecimals()
    {
        var act = _sut.GetValue("5.1239");
        act.Value.Should().Be(5.124);
    }

    [Fact]
    public void Parse_ShouldSReturnDefaultValue_WhenEmpty()
    {
        var act = _sut.GetValue(string.Empty);
        act.Value.Should().Be(default);
    }

    [Fact]
    public void Parse_ShouldReturnDefaultValue_WhenFail()
    {
        var act = _sut.GetValue("a");
        act.Value.Should().Be(default);
    }
}