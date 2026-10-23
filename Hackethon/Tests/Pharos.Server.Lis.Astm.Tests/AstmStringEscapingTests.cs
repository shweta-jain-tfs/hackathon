using Pharos.Server.Lis.Astm.Application.Messages;

namespace Pharos.Server.Lis.Astm.Tests;

public class AstmStringEscapingTests
{
    [Fact]
    public void EscapingPlainStringReturnSameString()
    {
        var s = "abc1234";

        var result = AstmFieldDelimiters.Default.AddEscape(s);
        result.Should().Be(s);

        var reverted = AstmFieldDelimiters.Default.RemoveEscape(result);
        reverted.Should().Be(s);
    }

    [Fact]
    public void EscapingFieldDelimiter()
    {
        var s = AstmFieldDelimiters.Default.Field.ToString();

        var result = AstmFieldDelimiters.Default.AddEscape(s);
        result.Should().Be("&F&");

        var reverted = AstmFieldDelimiters.Default.RemoveEscape(result);
        reverted.Should().Be(s);
    }

    [Fact]
    public void EscapingRepeatDelimiter()
    {
        var s = AstmFieldDelimiters.Default.Repeat.ToString();

        var result = AstmFieldDelimiters.Default.AddEscape(s);
        result.Should().Be("&R&");

        var reverted = AstmFieldDelimiters.Default.RemoveEscape(result);
        reverted.Should().Be(s);
    }

    [Fact]
    public void EscapingComponentDelimiter()
    {
        var s = AstmFieldDelimiters.Default.Component.ToString();

        var result = AstmFieldDelimiters.Default.AddEscape(s);
        result.Should().Be("&S&");

        var reverted = AstmFieldDelimiters.Default.RemoveEscape(result);
        reverted.Should().Be(s);
    }

    [Fact]
    public void EscapingEscapeDelimiter()
    {
        var s = AstmFieldDelimiters.Default.Escape.ToString();

        var result = AstmFieldDelimiters.Default.AddEscape(s);
        result.Should().Be("&E&");

        var reverted = AstmFieldDelimiters.Default.RemoveEscape(result);
        reverted.Should().Be(s);
    }

    [Fact]
    public void EscapingAllDelimiters()
    {
        var s = "&\\^|";

        var result = AstmFieldDelimiters.Default.AddEscape(s);
        result.Should().Be("&E&&R&&S&&F&");

        var reverted = AstmFieldDelimiters.Default.RemoveEscape(result);
        reverted.Should().Be(s);
    }

    [Fact]
    public void DeescapingCloseToEscapeCharacter()
    {
        var s = "data &E& escape";

        var result = AstmFieldDelimiters.Default.AddEscape(s);
        result.Should().Be("data &E&E&E& escape");

        var reverted = AstmFieldDelimiters.Default.RemoveEscape(result);
        reverted.Should().Be(s);
    }

    [Fact]
    public void DeescapingOrder()
    {
        var s = "data \\F\\ escape";

        var result = AstmFieldDelimiters.Default.AddEscape(s);
        result.Should().Be("data &R&F&R& escape");

        var reverted = AstmFieldDelimiters.Default.RemoveEscape(result);
        reverted.Should().Be(s);
    }

    [Fact]
    public void DeescapingUnknownEscapeStringAreIgnored()
    {
        var s = "data &N& escape";

        var result = AstmFieldDelimiters.Default.RemoveEscape(s);
        result.Should().Be(s);
    }
}