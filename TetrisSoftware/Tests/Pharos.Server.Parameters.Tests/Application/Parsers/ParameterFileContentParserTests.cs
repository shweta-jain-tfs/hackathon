using System.Text;

#pragma warning disable CS8625

namespace Pharos.Server.Parameters.Application.Parsers;

public class ParameterFileContentParserTests
{
    private readonly Fixture _fixture;
    private readonly ParameterFileContentParser _sut;

    public ParameterFileContentParserTests()
    {
        _fixture = new Fixture();
        _sut = new ParameterFileContentParser();
    }

    [Fact]
    public void Parse_WithNullAsContentAndFileName_ShouldThrowException()
    {
        var act = () => _sut.Parse(null, null);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Parse_WithEmptyContentAndNullFileName_ShouldThrowException()
    {
        var act = () => _sut.Parse(string.Empty, null);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Parse_WithBogusContentAndFileName_ShouldReturnEmptyFile()
    {
        var fileName = _fixture.Create<string>();

        var file = _sut.Parse("string.Empty", fileName);

        file.Name.Should().Be(fileName);
        file.Sections.Should().BeEmpty();
    }

    [Fact]
    public void Parse_WithTwoSectionsButNoData_ShouldReturnFileAccordingly()
    {
        var fileName = _fixture.Create<string>();
        var sb = new StringBuilder();
        sb.AppendLine("[Section 1]");
        sb.AppendLine("[Section 2]");
        var fileContent = sb.ToString();

        var file = _sut.Parse(fileContent, fileName);

        file.Name.Should().Be(fileName);
        file.Sections.Should().HaveCount(2);
    }

    [Fact]
    public void Parse_WithSectionsAndData_ShouldReturnFileAccordingly()
    {
        var fileName = _fixture.Create<string>();
        var sb = new StringBuilder();
        sb.AppendLine("[Section 1]");
        sb.AppendLine(BuildKeyWithValue("A", "2"));
        sb.AppendLine(BuildKeyWithValue());
        sb.AppendLine("[Section 2]");
        sb.AppendLine(_fixture.Create<string>());
        var fileContent = sb.ToString();

        var file = _sut.Parse(fileContent, fileName);

        file.Name.Should().Be(fileName);
        file.Sections.Should().HaveCount(2);

        var firstSection = file.Sections[0];
        firstSection.Parameters.Should().HaveCount(2);
        var firstData = firstSection.Parameters[0];
        firstData.Key.Should().Be("A");
        firstData.Value.Should().Be("2");
        firstData.HasKey.Should().BeTrue();

        var secondSection = file.Sections[1];
        secondSection.Parameters.Should().HaveCount(1);
        var secondData = secondSection.Parameters[0];
        secondData.Key.Should().BeEmpty();
        secondData.Value.Should().NotBeEmpty();
        secondData.HasKey.Should().BeFalse();
    }

    [Fact]
    public void Parse_WithLineMoreThanOneEqualSign_ShouldThrowException()
    {
        var sb = new StringBuilder();
        sb.AppendLine("[Section 1]");
        sb.AppendLine("A==2");
        Action act = () => _sut.Parse(sb.ToString(), null);
        act.Should().Throw<ArgumentOutOfRangeException>();

        sb.Clear();
        sb.AppendLine("[Section 1]");
        sb.AppendLine("A=1=2");
        act = () => _sut.Parse(sb.ToString(), null);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    private string BuildKeyWithValue()
    {
        var key = _fixture.Create<string>();
        var value = _fixture.Create<string>();
        return BuildKeyWithValue(key, value);
    }

    private static string BuildKeyWithValue(string key, string value)
    {
        return $"{key}={value}";
    }
}