using Pharos.Server.Lis.Astm.Application.Common;
using Pharos.Server.Lis.Astm.Application.Messages;

namespace Pharos.Server.Lis.Astm.Tests;

public class AstmFieldParserTests
{
    [Theory]
    [InlineData("int")]
    [InlineData("nullable-int")]
    [InlineData("short")]
    [InlineData("nullable-short")]
    [InlineData("date")]
    [InlineData("date-time")]
    [InlineData("date-only")]
    public void ParseInvalidField_WithErrorCollection_RecordsErrorWithoutThrowing(string fieldType)
    {
        var errors = new List<string>();
        var parser = new AstmFieldParser("INVALID", AstmFieldDelimiters.Default, 1, errors);

        var act = () => Parse(parser, fieldType);

        act.Should().NotThrow();
        errors.Should().ContainSingle();
    }

    [Theory]
    [InlineData("int")]
    [InlineData("nullable-int")]
    [InlineData("short")]
    [InlineData("nullable-short")]
    [InlineData("date")]
    [InlineData("date-time")]
    [InlineData("date-only")]
    public void ParseInvalidField_WithoutErrorCollection_PreservesStrictParsing(string fieldType)
    {
        var parser = new AstmFieldParser("INVALID", AstmFieldDelimiters.Default, 1);

        var act = () => Parse(parser, fieldType);

        act.Should().Throw<AstmException>();
    }

    [Fact]
    public void SubComponents_WithInvalidNumber_UsesParentErrorCollection()
    {
        var errors = new List<string>();
        var parser = new AstmFieldParser("1\\INVALID", AstmFieldDelimiters.Default, 1, errors);

        var values = parser.SubComponents(0).AsShortList();

        values.Should().Equal(1, 0);
        errors.Should().ContainSingle();
    }

    private static object? Parse(AstmFieldParser parser, string fieldType)
    {
        return fieldType switch
        {
            "int" => parser.IntOrZero(0),
            "nullable-int" => parser.IntOrNull(0),
            "short" => parser.ShortOrZero(0),
            "nullable-short" => parser.ShortOrNull(0),
            "date" => parser.AsDate(),
            "date-time" => parser.AsDateAndTime(),
            "date-only" => parser.AsDateOnly(),
            _ => throw new ArgumentException("Unknown field type", nameof(fieldType))
        };
    }
}
