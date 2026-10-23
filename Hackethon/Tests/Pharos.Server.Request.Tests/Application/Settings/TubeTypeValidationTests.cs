using System.ComponentModel.DataAnnotations;
using Pharos.Server.Application.Settings.Sample;

namespace Pharos.Server.Request.Application.Settings;

public class TubeTypeValidationTests
{
    [Theory]
    [InlineData("Short")]
    [InlineData("Medium Length Tube Name")]
    [InlineData("Serum Separator Tube")]
    public void Name_ShouldBeValid_WhenWithinMaxLength(string name)
    {
        // Arrange
        var tubeType = new TubeType
        {
            Name = name,
            TubeDefinitionId = 1
        };

        // Act
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(tubeType) { MemberName = nameof(TubeType.Name) };
        var isValid = Validator.TryValidateProperty(tubeType.Name, validationContext, validationResults);

        // Assert
        Assert.True(isValid);
        Assert.Empty(validationResults);
    }

    [Fact]
    public void Name_ShouldBeValid_WhenExactlyMaxLength()
    {
        // Arrange
        var tubeType = new TubeType
        {
            Name = new string('A', TubeType.MaxTubeTypeNameLength),
            TubeDefinitionId = 1
        };

        // Act
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(tubeType) { MemberName = nameof(TubeType.Name) };
        var isValid = Validator.TryValidateProperty(tubeType.Name, validationContext, validationResults);

        // Assert
        Assert.True(isValid);
        Assert.Empty(validationResults);
    }

    [Fact]
    public void Name_ShouldBeInvalid_WhenExceedsMaxLength()
    {
        // Arrange
        var tubeType = new TubeType
        {
            Name = new string('A', TubeType.MaxTubeTypeNameLength + 1),
            TubeDefinitionId = 1
        };

        // Act
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(tubeType) { MemberName = nameof(TubeType.Name) };
        var isValid = Validator.TryValidateProperty(tubeType.Name, validationContext, validationResults);

        // Assert
        Assert.False(isValid);
        Assert.Single(validationResults);
        Assert.Contains($"field Name must be a string or array type with a maximum length of '{TubeType.MaxTubeTypeNameLength}'",
            validationResults[0].ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Name_ShouldBeInvalid_WhenNull()
    {
        // Arrange
        var tubeType = new TubeType
        {
            Name = null!,
            TubeDefinitionId = 1
        };

        // Act
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(tubeType) { MemberName = nameof(TubeType.Name) };
        var isValid = Validator.TryValidateProperty(tubeType.Name, validationContext, validationResults);

        // Assert
        Assert.False(isValid);
        Assert.NotEmpty(validationResults);
    }

    [Fact]
    public void Name_ShouldBeInvalid_WhenEmpty()
    {
        // Arrange
        var tubeType = new TubeType
        {
            Name = string.Empty,
            TubeDefinitionId = 1
        };

        // Act
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(tubeType) { MemberName = nameof(TubeType.Name) };
        var isValid = Validator.TryValidateProperty(tubeType.Name, validationContext, validationResults);

        // Assert
        Assert.False(isValid);
        Assert.NotEmpty(validationResults);
    }
}
