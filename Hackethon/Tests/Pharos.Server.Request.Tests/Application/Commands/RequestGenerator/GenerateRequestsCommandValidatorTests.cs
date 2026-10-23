using System.Text;
using FluentValidation.TestHelper;
using Pharos.Server.Localization;

namespace Pharos.Server.Request.Application.Commands.RequestGenerator;

public class GenerateRequestsCommandValidatorTests
{
    private static readonly CompositeFormat _amountToGenerateFormat = CompositeFormat.Parse(Translations.RequestGenerator_AmountToGenerate);
    private static readonly CompositeFormat _preDilutionFactorFormat = CompositeFormat.Parse(Translations.RequestGenerator_PreDilutionFactor);
    private static readonly CompositeFormat _numberOfDigitsFormat = CompositeFormat.Parse(Translations.RequestGenerator_NumberOfDigits);
    private static readonly CompositeFormat _sampleIdStartOffsetFormat = CompositeFormat.Parse(Translations.RequestGenerator_SampleIdStartOffset);
    private static readonly CompositeFormat _trayStartOffsetFormat = CompositeFormat.Parse(Translations.RequestGenerator_TrayStartOffset);
    private static readonly CompositeFormat _samplesPerTrayFormat = CompositeFormat.Parse(Translations.RequestGenerator_SamplesPerTray);
    private static readonly CompositeFormat _replicatesFormat = CompositeFormat.Parse(Translations.RequestGenerator_Replicates);
    private readonly GenerateRequestsCommandValidator _validator;

    public GenerateRequestsCommandValidatorTests()
    {
        _validator = new GenerateRequestsCommandValidator();
    }

    [Trait("ModuleRequirement", "MR_5834")]
    [Trait("ProductRequirement", "PR_1721")]
    [Fact]
    public void Should_PassValidation_When_AllFieldsAreValid()
    {
        // Arrange
        var command = BuildValidCommand();

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Trait("ModuleRequirement", "MR_5833")]
    [Trait("ProductRequirement", "PR_1721")]
    [Fact]
    public void Should_HaveError_When_TestsIsEmpty()
    {
        // Arrange
        var command = BuildValidCommand([]);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor("GenerateRequestsDto.Tests")
              .WithErrorMessage(Translations.RequestGenerator_AtLeastOneTest);
    }

    [Trait("ModuleRequirement", "MR_5834")]
    [Trait("ProductRequirement", "PR_1721")]
    [Theory]
    [InlineData(0)]
    [InlineData(10000)]
    public void Should_HaveError_When_AmountToGenerate_IsOutOfRange(int amount)
    {
        // Arrange
        var command = BuildValidCommand(amountToGenerate: amount);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor("GenerateRequestsDto.AmountToGenerate")
              .WithErrorMessage(string.Format(null, _amountToGenerateFormat, 1, 9999));
    }

    [Trait("ModuleRequirement", "MR_5834")]
    [Trait("ProductRequirement", "PR_1721")]
    [Theory]
    [InlineData(1)]
    [InlineData(9999)]
    public void Should_NotHaveError_When_AmountToGenerate_IsWithinRange(int amount)
    {
        // Arrange
        var command = BuildValidCommand(amountToGenerate: amount);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor("GenerateRequestsDto.AmountToGenerate");
    }

    [Trait("ModuleRequirement", "MR_5834")]
    [Trait("ProductRequirement", "PR_1721")]
    [Theory]
    [InlineData(0)]
    [InlineData(1001)]
    public void Should_HaveError_When_PreDilutionFactor_IsOutOfRange(int factor)
    {
        // Arrange
        var command = BuildValidCommand(preDilutionFactor: factor);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor("GenerateRequestsDto.PreDilutionFactor")
              .WithErrorMessage(string.Format(null, _preDilutionFactorFormat, 1, 1000));
    }

    [Trait("ModuleRequirement", "MR_5828")]
    [Trait("ProductRequirement", "PR_1721")]
    [Fact]
    public void Should_HaveError_When_HeadingCharacters_ExceedsMaxLength()
    {
        // Arrange
        var command = BuildValidCommand(sampleIdHeadingCharacters: new string('A', 8));

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor("GenerateRequestsDto.SampleIdHeadingCharacters");
    }

    [Trait("ModuleRequirement", "MR_5828")]
    [Trait("ProductRequirement", "PR_1721")]
    [Theory]
    [InlineData("AB$CD")]
    [InlineData("S-001")]
    [InlineData("X_Y")]
    [InlineData("A.B")]
    public void Should_NotHaveError_When_HeadingCharacters_ContainSpecialCharacters(string headingCharacters)
    {
        // Arrange
        var command = BuildValidCommand(sampleIdHeadingCharacters: headingCharacters);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor("GenerateRequestsDto.SampleIdHeadingCharacters");
    }

    [Trait("ModuleRequirement", "MR_5828")]
    [Trait("ProductRequirement", "PR_1721")]
    [Fact]
    public void Should_HaveError_When_TrailingCharacters_ExceedsMaxLength()
    {
        // Arrange
        var command = BuildValidCommand(sampleIdTrailingCharacters: new string('Z', 8));

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor("GenerateRequestsDto.SampleIdTrailingCharacters");
    }

    [Trait("ModuleRequirement", "MR_5828")]
    [Trait("ProductRequirement", "PR_1721")]
    [Theory]
    [InlineData("Z$99")]
    [InlineData("-end")]
    [InlineData("_T")]
    [InlineData(".B")]
    public void Should_NotHaveError_When_TrailingCharacters_ContainSpecialCharacters(string trailingCharacters)
    {
        // Arrange
        var command = BuildValidCommand(sampleIdTrailingCharacters: trailingCharacters);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor("GenerateRequestsDto.SampleIdTrailingCharacters");
    }

    [Trait("ModuleRequirement", "MR_5828")]
    [Trait("ProductRequirement", "PR_1721")]
    [Theory]
    [InlineData(0)]
    [InlineData(17)]
    public void Should_HaveError_When_NumberOfDigits_IsOutOfRange(int digits)
    {
        // Arrange
        var command = BuildValidCommand(sampleIdNumberOfDigits: digits);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor("GenerateRequestsDto.SampleIdNumberOfDigits")
              .WithErrorMessage(string.Format(null, _numberOfDigitsFormat, 1, 16));
    }

    [Trait("ModuleRequirement", "MR_5828")]
    [Trait("ProductRequirement", "PR_1721")]
    [Theory]
    [InlineData(-1)]
    [InlineData(10000000)]
    public void Should_HaveError_When_SampleIdStartOffset_IsOutOfRange(int offset)
    {
        // Arrange
        var command = BuildValidCommand(sampleIdStartOffset: offset);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor("GenerateRequestsDto.SampleIdStartOffset")
              .WithErrorMessage(string.Format(null, _sampleIdStartOffsetFormat, 0, 9999999));
    }

    [Trait("ModuleRequirement", "MR_5830")]
    [Trait("ProductRequirement", "PR_1721")]
    [Theory]
    [InlineData(-1)]
    [InlineData(10000)]
    public void Should_HaveError_When_TrayStartOffset_IsOutOfRange_AndTraysEnabled(int offset)
    {
        // Arrange
        var command = BuildValidCommand(placeSamplesInTrays: true, traysStartOffset: offset, samplesPerTray: 10);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor("GenerateRequestsDto.TraysStartOffset")
              .WithErrorMessage(string.Format(null, _trayStartOffsetFormat, 0, 9999));
    }

    [Trait("ModuleRequirement", "MR_5830")]
    [Trait("ProductRequirement", "PR_1721")]
    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void Should_HaveError_When_SamplesPerTray_IsOutOfRange_AndTraysEnabled(int samplesPerTray)
    {
        // Arrange
        var command = BuildValidCommand(placeSamplesInTrays: true, traysStartOffset: 0, samplesPerTray: samplesPerTray);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor("GenerateRequestsDto.SamplesPerTray")
              .WithErrorMessage(string.Format(null, _samplesPerTrayFormat, 1, 50));
    }

    [Trait("ModuleRequirement", "MR_5830")]
    [Trait("ProductRequirement", "PR_1721")]
    [Fact]
    public void Should_NotHaveError_When_TrayConfigIsInvalid_ButTraysDisabled()
    {
        // Arrange
        var command = BuildValidCommand(placeSamplesInTrays: false, traysStartOffset: -999, samplesPerTray: 999);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Trait("ModuleRequirement", "MR_5831")]
    [Trait("ProductRequirement", "PR_1721")]
    [Fact]
    public void Should_HaveError_When_TestMethodName_IsEmpty()
    {
        // Arrange
        var command = BuildValidCommand([
            new TestDetails { MethodName = "", TestName = "TST1", Replicates = 1, InstrumentDilutionFactor = 1 }
        ]);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor("GenerateRequestsDto.Tests[0].MethodName")
              .WithErrorMessage(Translations.RequestGenerator_MethodNameRequired);
    }

    [Trait("ModuleRequirement", "MR_5831")]
    [Trait("ProductRequirement", "PR_1721")]
    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public void Should_HaveError_When_TestReplicates_IsOutOfRange(int replicates)
    {
        // Arrange
        var command = BuildValidCommand([
            new TestDetails { MethodName = "MTH1", TestName = "TST1", Replicates = replicates, InstrumentDilutionFactor = 1 }
        ]);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor("GenerateRequestsDto.Tests[0].Replicates")
              .WithErrorMessage(string.Format(null, _replicatesFormat, 1, 20));
    }

    // Helper method
    private static GenerateRequestsCommand BuildValidCommand(
        List<TestDetails>? tests = null,
        string sampleIdHeadingCharacters = "S",
        int sampleIdNumberOfDigits = 6,
        int sampleIdStartOffset = 0,
        string sampleIdTrailingCharacters = "",
        bool placeSamplesInTrays = false,
        int traysStartOffset = 0,
        int samplesPerTray = 10,
        int amountToGenerate = 10,
        int preDilutionFactor = 1) => new(new GenerateRequestsDto
        {
            SampleIdHeadingCharacters = sampleIdHeadingCharacters,
            SampleIdNumberOfDigits = sampleIdNumberOfDigits,
            SampleIdStartOffset = sampleIdStartOffset,
            SampleIdTrailingCharacters = sampleIdTrailingCharacters,
            PlaceSamplesInTrays = placeSamplesInTrays,
            TraysStartOffset = traysStartOffset,
            SamplesPerTray = samplesPerTray,
            AmountToGenerate = amountToGenerate,
            PreDilutionFactor = preDilutionFactor,
            Tests = tests ??
            [
                new TestDetails
                {
                    MethodName = "MTH1",
                    TestName = "TST1",
                    Replicates = 1,
                    InstrumentDilutionFactor = 1
                }
            ]
        });
}

