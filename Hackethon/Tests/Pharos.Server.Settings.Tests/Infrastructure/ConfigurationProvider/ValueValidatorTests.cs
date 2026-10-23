using System.ComponentModel.DataAnnotations;
using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

public class ValueValidatorTests
{
    #region Bool Tests

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsValidValue_ShouldReturnTrue_ForBoolValues(bool value)
    {
        // Arrange
        var property = typeof(TestClass).GetProperty(nameof(TestClass.BoolProp));

        // Act
        var result = ValueValidator.IsValidValue(property, value);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidValue_ShouldReturnTrue_ForNullableBoolWithValue()
    {
        // Arrange
        var property = typeof(TestClass).GetProperty(nameof(TestClass.NullableBoolProp));

        // Act
        var result = ValueValidator.IsValidValue(property, true);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region DateTimeOffset Tests

    [Fact]
    public void IsValidValue_ShouldReturnTrue_ForDateTimeOffsetValue()
    {
        // Arrange
        var property = typeof(TestClass).GetProperty(nameof(TestClass.DateTimeProp));
        var value = new DateTimeOffset(2024, 6, 12, 0, 0, 0, TimeSpan.Zero);

        // Act
        var result = ValueValidator.IsValidValue(property, value);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidValue_ShouldReturnTrue_ForNullableDateTimeOffsetPropertyType()
    {
        // Arrange
        var property = typeof(TestClass).GetProperty(nameof(TestClass.NullableDateTimeProp));
        var value = new DateTimeOffset(2024, 6, 12, 0, 0, 0, TimeSpan.Zero);

        // Act
        var result = ValueValidator.IsValidValue(property, value);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Enum Tests

    [Fact]
    public void IsValidValue_ShouldReturnTrue_ForEnumValue()
    {
        // Arrange
        var property = typeof(TestClass).GetProperty(nameof(TestClass.EnumProp));
        var value = TestEnum.Value2;

        // Act
        var result = ValueValidator.IsValidValue(property, value);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Int Tests

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    public void IsValidValue_ShouldReturnTrue_ForValidIntWithinRange(int value)
    {
        // Arrange
        var property = typeof(TestClass).GetProperty(nameof(TestClass.IntWithRange));

        // Act
        var result = ValueValidator.IsValidValue(property, value);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void IsValidValue_ShouldReturnFalse_ForIntOutsideRange(int value)
    {
        // Arrange
        var property = typeof(TestClass).GetProperty(nameof(TestClass.IntWithRange));

        // Act
        var result = ValueValidator.IsValidValue(property, value);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidValue_ShouldReturnTrue_ForNullableIntWithValue()
    {
        // Arrange
        var property = typeof(TestClass).GetProperty(nameof(TestClass.NullableIntProp));

        // Act
        var result = ValueValidator.IsValidValue(property, 42);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidValue_ShouldReturnTrue_ForNullValueWithNullableIntProperty()
    {
        // Arrange
        var property = typeof(TestClass).GetProperty(nameof(TestClass.NullableIntProp));

        // Act
        var result = ValueValidator.IsValidValue(property, null);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidValue_ShouldReturnFalse_ForInvalidIntString()
    {
        // Arrange
        var property = typeof(TestClass).GetProperty(nameof(TestClass.IntProp));

        // Act
        var result = ValueValidator.IsValidValue(property, "not_a_number");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Double Tests

    [Theory]
    [InlineData(0.0)]
    [InlineData(50.5)]
    [InlineData(100.0)]
    public void IsValidValue_ShouldReturnTrue_ForValidDoubleWithinRange(double value)
    {
        // Arrange
        var property = typeof(TestClass).GetProperty(nameof(TestClass.DoubleWithRange));

        // Act
        var result = ValueValidator.IsValidValue(property, value);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(100.1)]
    public void IsValidValue_ShouldReturnFalse_ForDoubleOutsideRange(double value)
    {
        // Arrange
        var property = typeof(TestClass).GetProperty(nameof(TestClass.DoubleWithRange));

        // Act
        var result = ValueValidator.IsValidValue(property, value);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidValue_ShouldReturnTrue_ForNullableDoubleWithValue()
    {
        // Arrange
        var property = typeof(TestClass).GetProperty(nameof(TestClass.NullableDoubleProp));

        // Act
        var result = ValueValidator.IsValidValue(property, 42.5);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidValue_ShouldReturnTrue_ForNullValueWithNullableDoubleProperty()
    {
        // Arrange
        var property = typeof(TestClass).GetProperty(nameof(TestClass.NullableDoubleProp));

        // Act
        var result = ValueValidator.IsValidValue(property, null);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidValue_ShouldReturnFalse_ForInvalidDoubleString()
    {
        // Arrange
        var property = typeof(TestClass).GetProperty(nameof(TestClass.DoubleProp));

        // Act
        var result = ValueValidator.IsValidValue(property, "not_a_number");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region String Tests

    [Theory]
    [InlineData("short")]
    [InlineData("exactly10!")]
    [InlineData("")]
    public void IsValidValue_ShouldReturnTrue_ForStringWithinMaxLength(string value)
    {
        // Arrange
        var property = typeof(TestClass).GetProperty(nameof(TestClass.StringWithMaxLength));

        // Act
        var result = ValueValidator.IsValidValue(property, value);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidValue_ShouldReturnFalse_ForStringExceedingMaxLength()
    {
        // Arrange
        var property = typeof(TestClass).GetProperty(nameof(TestClass.StringWithMaxLength));
        var value = "this_is_too_long";

        // Act
        var result = ValueValidator.IsValidValue(property, value);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidValue_ShouldReturnTrue_ForNullString()
    {
        // Arrange
        var property = typeof(TestClass).GetProperty(nameof(TestClass.StringProp));

        // Act
        var result = ValueValidator.IsValidValue(property, null);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("valid value", true)]
    [InlineData("invalid<value", false)]
    [InlineData("invalid>value", false)]
    [InlineData("", true)]
    public void IsValidValue_ShouldValidateDisallowedCharacters(string value, bool expectedValid)
    {
        var property = typeof(TestClass).GetProperty(nameof(TestClass.StringWithDisallowedCharacters));

        var result = ValueValidator.IsValidValue(property, value);

        result.Should().Be(expectedValid);
    }

    #endregion

    #region TimeOnly Tests

    [Fact]
    public void IsValidValue_ShouldReturnTrue_ForTimeOnlyPropertyType()
    {
        // Arrange
        var property = typeof(TestClass).GetProperty(nameof(TestClass.TimeOnlyProp));
        var value = new TimeOnly(14, 30, 0);

        // Act
        var result = ValueValidator.IsValidValue(property, value);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Null Value Tests

    [Fact]
    public void IsValidValue_ShouldReturnTrue_ForNullValue()
    {
        // Arrange
        var property = typeof(TestClass).GetProperty(nameof(TestClass.StringProp));

        // Act
        var result = ValueValidator.IsValidValue(property, null);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Unsupported Type Tests

    [Fact]
    public void IsValidValue_ShouldThrowArgumentException_ForUnsupportedType()
    {
        // Arrange
        var property = typeof(TestClass).GetProperty(nameof(TestClass.UnsupportedProp));
        var value = new object();

        // Act & Assert
        var act = () => ValueValidator.IsValidValue(property, value);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Type [System.Object] of value * is not supported");
    }

    #endregion

    #region SettingsRange Tests for TubeDefinitionIds 

    [Trait("ProductRequirement", "PR_315")]
    [Trait("ModuleRequirement", "MR_3792")]
    [Theory]
    [InlineData(1, true)]
    [InlineData(5, true)]
    [InlineData(0, false)]
    [InlineData(6, false)]
    public void IsValidValue_ShouldValidate_SettingsRange_For_TubeDefinitionIds(int value, bool expectedValid)
    {
        // Arrange
        var property = typeof(TestClass).GetProperty(nameof(TestClass.IntWithRange1To5));

        // Act
        var result = ValueValidator.IsValidValue(property, value);

        // Assert
        result.Should().Be(expectedValid);
    }

    #endregion

    #region Test Class

    private class TestClass
    {
        public bool BoolProp { get; set; }
        public bool? NullableBoolProp { get; set; }
        public DateTimeOffset DateTimeProp { get; set; }
        public DateTimeOffset? NullableDateTimeProp { get; set; }
        public TestEnum EnumProp { get; set; }

        public int IntProp { get; set; }
        public int? NullableIntProp { get; set; }

        [SettingsRange(0, 100)]
        public int IntWithRange { get; set; }

        [SettingsRange(1, 5)]
        public int IntWithRange1To5 { get; set; }

        public double DoubleProp { get; set; }
        public double? NullableDoubleProp { get; set; }

        [SettingsRange(0.0, 100.0)]
        public double DoubleWithRange { get; set; }

        public string? StringProp { get; set; }

        [MaxLength(10)]
        public string? StringWithMaxLength { get; set; }

        [DisallowedCharacters(@"[<>]")]
        public string? StringWithDisallowedCharacters { get; set; }

        public TimeOnly TimeOnlyProp { get; set; }

        public object? UnsupportedProp { get; set; }
    }

    private enum TestEnum
    {
        Value1,
        Value2
    }

    #endregion
}
