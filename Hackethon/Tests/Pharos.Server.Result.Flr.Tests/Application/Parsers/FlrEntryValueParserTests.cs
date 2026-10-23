using FluentAssertions;
using Google.Protobuf.WellKnownTypes;

namespace Pharos.Server.Result.Flr.Application.Parsers;

public class FlrEntryValueParserTests
{
    [Fact]
    public void GetString_ReturnsValue_WhenColumnExists()
    {
        // Act
        var result = GetString("abc");

        // Assert
        result.Should().Be("abc");
    }

    [Fact]
    public void GetString_ReturnsDefault_WhenColumnMissingAndCanBeEmpty()
    {
        var values = new[] { "abc" };

        // Act
        var result = values.GetString(new Dictionary<string, FlrEntryColumn>(), "Missing", defaultValue: "fallback");

        // Assert
        result.Should().Be("fallback");
    }

    [Fact]
    public void GetString_ThrowsFormatException_WhenEmptyAndRequired()
    {
        // Act
        var act = () => GetString("", canBeEmpty: false);

        // Assert
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void GetString_ThrowsArgumentException_WhenMissingAndRequired()
    {
        var values = new[] { "abc" };

        // Act
        var act = () => values.GetString(new Dictionary<string, FlrEntryColumn>(), "Missing", canBeEmpty: false);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetInt_ReturnsParsedValue_WhenValid()
    {
        // Act
        var result = GetInt("42");

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public void GetInt_ReturnsDefault_WhenInvalid()
    {
        // Act
        var result = GetInt("invalid", defaultValue: 7);

        // Assert
        result.Should().Be(7);
    }

    [Fact]
    public void GetInt_ThrowsFormatException_WhenRequiredAndInvalid()
    {
        // Act
        var act = () => GetInt("invalid", canBeEmpty: false);

        // Assert
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void TryGetInt_ReturnsParsedValue_WhenValid()
    {
        // Act
        var result = TryGetInt("51");

        // Assert
        result.Should().Be(51);
    }

    [Fact]
    public void TryGetInt_ReturnsDefault_WhenInvalid()
    {
        // Act
        var result = TryGetInt("invalid", defaultValue: 9);

        // Assert
        result.Should().Be(9);
    }

    [Theory]
    [InlineData("1.25", 1.25)]
    [InlineData("1,25", 1.25)]
    public void GetDouble_ParsesDotAndCommaDecimalSeparators(string input, double expected)
    {
        // Act
        var result = GetDouble(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void GetDouble_ReturnsDefault_WhenInvalid()
    {
        // Act
        var result = GetDouble("invalid", defaultValue: 3.14);

        // Assert
        result.Should().Be(3.14);
    }

    [Theory]
    [InlineData("TRUE", true)]
    [InlineData("true", true)]
    [InlineData("1", true)]
    [InlineData("FALSE", false)]
    [InlineData("0", false)]
    [InlineData("other", false)]
    public void GetBool_ParsesSupportedValues(string input, bool expected)
    {
        // Act
        var result = GetBool(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void GetBool_ReturnsDefault_WhenEmpty()
    {
        // Act
        var result = GetBool("", defaultValue: true);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GetTimestamp_ReturnsNull_WhenEmpty()
    {
        // Act
        var result = GetTimestamp("");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetTimestamp_ParsesAndConvertsToUtc()
    {
        // Act
        var result = GetTimestamp("2024-01-01T10:00:00+02:00");

        // Assert
        result.Should().NotBeNull();
        result.ToDateTimeOffset().Should().Be(new DateTimeOffset(2024, 1, 1, 8, 0, 0, TimeSpan.Zero));
    }

    [Theory]
    [InlineData("ERW_A88", 2, 90, 1, 88)]
    [InlineData("A88", 2, 90, 1, 88)]
    [InlineData("IRW_C49", 4, 78, 3, 49)]
    [InlineData("IRW_C01", 4, 78, 3, 1)]
    [InlineData("C2", 4, 78, 3, 2)]
    public void GetWheelPosition_ParsesSupportedFormats(
        string position,
        int maxRow,
        int maxSpoke,
        int expectedRow,
        int expectedSpoke)
    {
        // Act
        var (row, spoke) = GetWheelPosition(position, maxRow, maxSpoke);

        // Assert
        row.Should().Be(expectedRow);
        spoke.Should().Be(expectedSpoke);
    }

    [Theory]
    [InlineData("XYZ_A88", 2, 90)]
    [InlineData("IRW_E49", 4, 78)]
    [InlineData("ERW_A91", 2, 90)]
    [InlineData("ERW_A0", 2, 90)]
    [InlineData("ERW_AAA", 2, 90)]
    [InlineData("ERW_", 2, 90)]
    public void GetWheelPosition_InvalidFormatOrRange_ThrowsFormatException(
        string position,
        int maxRow,
        int maxSpoke)
    {
        // Act
        var act = () => GetWheelPosition(position, maxRow, maxSpoke);

        // Assert
        act.Should().Throw<FormatException>();
    }

    [Theory]
    [InlineData("C3_1", 1)]
    [InlineData("FS_B1, C3_1", 1)]
    [InlineData("FS_A9, C7_8", 8)]
    [InlineData("FS_D12, C1_15", 15)]
    public void GetCartridgeCell_ParsesCellFromConjugateLocationOrCartridgeFormat(string location, int expected)
    {
        // Act
        var result = GetCartridgeCell(location);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void GetCartridgeCell_ReturnsDefault_WhenEmptyAndCanBeEmpty()
    {
        // Act
        var result = GetCartridgeCell("", defaultValue: 5);

        // Assert
        result.Should().Be(5);
    }

    [Fact]
    public void GetCartridgeCell_ThrowsFormatException_WhenEmptyAndRequired()
    {
        // Act
        var act = () => GetCartridgeCell("", canBeEmpty: false);

        // Assert
        act.Should().Throw<FormatException>();
    }

    [Theory]
    [InlineData("C11")]
    [InlineData("C1_0")]
    [InlineData("C1_X")]
    [InlineData("C1_")]
    public void GetCartridgeCell_InvalidFormatOrRange_ThrowsFormatException(string location)
    {
        // Act
        var act = () => GetCartridgeCell(location);

        // Assert
        act.Should().Throw<FormatException>();
    }

    private static (int Row, int Spoke) GetWheelPosition(string position, int maxRow, int maxSpoke)
    {
        var (values, columns) = CreateInput(position, "WheelPosition");
        return values.GetWheelPosition(columns, "WheelPosition", maxRow, maxSpoke);
    }

    private static string GetString(string value, bool canBeEmpty = true, string defaultValue = "")
    {
        var (values, columns) = CreateInput(value);
        return values.GetString(columns, "Value", canBeEmpty, defaultValue);
    }

    private static int GetInt(string value, bool canBeEmpty = true, int defaultValue = 0)
    {
        var (values, columns) = CreateInput(value);
        return values.GetInt(columns, "Value", canBeEmpty, defaultValue);
    }

    private static int? TryGetInt(string value, int? defaultValue = null)
    {
        var (values, columns) = CreateInput(value);
        return values.TryGetInt(columns, "Value", defaultValue);
    }

    private static double GetDouble(string value, double defaultValue = 0.0)
    {
        var (values, columns) = CreateInput(value);
        return values.GetDouble(columns, "Value", defaultValue);
    }

    private static bool GetBool(string value, bool defaultValue = false)
    {
        var (values, columns) = CreateInput(value);
        return values.GetBool(columns, "Value", defaultValue);
    }

    private static Timestamp? GetTimestamp(string value)
    {
        var (values, columns) = CreateInput(value);
        return values.GetTimestamp(columns, "Value");
    }

    private static int GetCartridgeCell(string location, bool canBeEmpty = true, int defaultValue = 1)
    {
        var (values, columns) = CreateInput(location, "CartridgeLocation");
        return values.GetCartridgeCell(columns, "CartridgeLocation", canBeEmpty, defaultValue);
    }

    private static (string[] Values, IReadOnlyDictionary<string, FlrEntryColumn> Columns) CreateInput(string value, string columnName = "Value")
    {
        return (new[] { value }, new Dictionary<string, FlrEntryColumn> { [columnName] = new FlrEntryColumn(columnName, 0) });
    }
}
