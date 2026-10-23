using FluentAssertions;
using Microsoft.Extensions.Localization;
using Moq;
using Pharos.Client.App.Parameters.Common.Models;
using Pharos.Client.App.Parameters.Reflex.ViewModels;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using Xunit;

namespace Pharos.Client.App.Parameters.Tests.ViewModels;

public class ReflexConditionValidatorTests
{
    private readonly Mock<IMethodTestDataSource> _methodTestDataSourceMock;
    private readonly ReflexConditionValidator _sut;

    public ReflexConditionValidatorTests()
    {
        var _localizerMock = new Mock<IStringLocalizer<Translations>>();

        // Setup all localization keys used by the validator
        _localizerMock.Setup(x => x["ReflexDetailPage_TestNameRequired"])
            .Returns(new LocalizedString("ReflexDetailPage_TestNameRequired", "Test name is required"));

        _localizerMock.Setup(x => x["ReflexDetailPage_ResultTypeRequired"])
            .Returns(new LocalizedString("ReflexDetailPage_ResultTypeRequired", "Result type is required"));

        _localizerMock.Setup(x => x["ReflexDetailPage_ComparerRequired"])
            .Returns(new LocalizedString("ReflexDetailPage_ComparerRequired", "Comparer is required"));

        _localizerMock.Setup(x => x["ReflexDetailPage_ValueRequired"])
            .Returns(new LocalizedString("ReflexDetailPage_ValueRequired", "Value is required"));

        _localizerMock.Setup(x => x["ReflexDetailPage_MinValueRequired"])
            .Returns(new LocalizedString("ReflexDetailPage_MinValueRequired", "Minimum value is required"));

        _localizerMock.Setup(x => x["ReflexDetailPage_MaxValueRequired"])
            .Returns(new LocalizedString("ReflexDetailPage_MaxValueRequired", "Maximum value is required"));

        _localizerMock.Setup(x => x["ReflexDetailPage_MinValueMustBeLessThanMaxValue"])
            .Returns(new LocalizedString("ReflexDetailPage_MinValueMustBeLessThanMaxValue", "The minimum value must be less than the maximum value"));

        _localizerMock.Setup(x => x["ReflexDetailPage_ClassValueRequired"])
            .Returns(new LocalizedString("ReflexDetailPage_ClassValueRequired", "Class value is required"));

        _localizerMock.Setup(x => x["ReflexDetailPage_CutOffValueRequired"])
            .Returns(new LocalizedString("ReflexDetailPage_CutOffValueRequired", "CutOff value is required"));

        _localizerMock.Setup(x => x["ReflexDetailPage_RuValueRequired"])
            .Returns(new LocalizedString("ReflexDetailPage_RuValueRequired", "RU value is required"));

        _localizerMock.Setup(x => x["ReflexDetailPage_QuotientValueRequired"])
            .Returns(new LocalizedString("ReflexDetailPage_QuotientValueRequired", "Quotient value is required"));

        _localizerMock.Setup(x => x["ReflexDetailPage_ValueOutOfRange"])
            .Returns(new LocalizedString("ReflexDetailPage_ValueOutOfRange", "Value must be between {0} and {1}"));

        _localizerMock.Setup(x => x["ReflexDetailPage_MinValueOutOfRange"])
            .Returns(new LocalizedString("ReflexDetailPage_MinValueOutOfRange", "Minimum value must be between {0} and {1}"));

        _localizerMock.Setup(x => x["ReflexDetailPage_MaxValueOutOfRange"])
            .Returns(new LocalizedString("ReflexDetailPage_MaxValueOutOfRange", "Maximum value must be between {0} and {1}"));

        _methodTestDataSourceMock = new Mock<IMethodTestDataSource>();

        _sut = new ReflexConditionValidator(_localizerMock.Object);
    }

    [Fact]
    public void Validate_Should_Pass_When_UpperLimitGreaterThanLowerLimit_ForConcentration()
    {
        // Arrange
        var model = new ReflexCondition
        {
            CompareType = CompareType.Between,
            ResultType = ResultType.Concentration,
            ConcentrationCompareValue = 1.0,
            ConcentrationCompareValueUpperLimit = 5.0,
            TestName = "Test",
            MethodName = "Method",
            ReportingConcentration = true,
            ClassOptions = new List<string>(),
            CutOffOptions = new List<string>()
        };
        using var viewModel = new ReflexConditionViewModel(model, _methodTestDataSourceMock.Object, true);

        // Act
        var result = _sut.Validate(viewModel);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Trait("ProductRequirement", "PR_339")]
    [Trait("ModuleRequirement", "MR_3761")]
    [Fact]
    public void Validate_Should_Fail_When_UpperLimitNotGreaterThanLowerLimit_ForConcentration()
    {
        // Arrange
        var model = new ReflexCondition
        {
            CompareType = CompareType.Between,
            ResultType = ResultType.Concentration,
            ConcentrationCompareValue = 5.0,
            ConcentrationCompareValueUpperLimit = 3.0,
            TestName = "Test",
            MethodName = "Method",
            ReportingConcentration = true,
            ClassOptions = new List<string>(),
            CutOffOptions = new List<string>()
        };
        using var viewModel = new ReflexConditionViewModel(model, _methodTestDataSourceMock.Object, true);

        // Act
        var result = _sut.Validate(viewModel);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "The minimum value must be less than the maximum value");
    }

    [Fact]
    public void Validate_Should_Fail_When_UpperLimitEqualsLowerLimit_ForConcentration()
    {
        // Arrange
        var model = new ReflexCondition
        {
            CompareType = CompareType.Between,
            ResultType = ResultType.Concentration,
            ConcentrationCompareValue = 5.0,
            ConcentrationCompareValueUpperLimit = 5.0,
            TestName = "Test",
            MethodName = "Method",
            ReportingConcentration = true,
            ClassOptions = new List<string>(),
            CutOffOptions = new List<string>()
        };
        using var viewModel = new ReflexConditionViewModel(model, _methodTestDataSourceMock.Object, true);

        // Act
        var result = _sut.Validate(viewModel);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "The minimum value must be less than the maximum value");
    }

    [Fact]
    public void Validate_Should_Pass_When_UpperLimitGreaterThanLowerLimit_ForConcentrationRaw()
    {
        // Arrange
        var model = new ReflexCondition
        {
            CompareType = CompareType.Between,
            ResultType = ResultType.ConcentrationRaw,
            ConcentrationRawCompareValue = 100.0,
            ConcentrationRawCompareValueUpperLimit = 500.0,
            TestName = "Test",
            MethodName = "Method",
            ReportingConcentration = true,
            ClassOptions = new List<string>(),
            CutOffOptions = new List<string>()
        };
        using var viewModel = new ReflexConditionViewModel(model, _methodTestDataSourceMock.Object, true);

        // Act
        var result = _sut.Validate(viewModel);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Trait("ProductRequirement", "PR_339")]
    [Trait("ModuleRequirement", "MR_3761")]
    [Fact]
    public void Validate_Should_Fail_When_UpperLimitNotGreaterThanLowerLimit_ForConcentrationRaw()
    {
        // Arrange
        var model = new ReflexCondition
        {
            CompareType = CompareType.Between,
            ResultType = ResultType.ConcentrationRaw,
            ConcentrationRawCompareValue = 500.0,
            ConcentrationRawCompareValueUpperLimit = 100.0,
            TestName = "Test",
            MethodName = "Method",
            ReportingConcentration = true,
            ClassOptions = new List<string>(),
            CutOffOptions = new List<string>()
        };
        using var viewModel = new ReflexConditionViewModel(model, _methodTestDataSourceMock.Object, true);

        // Act
        var result = _sut.Validate(viewModel);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "The minimum value must be less than the maximum value");
    }

    [Trait("ProductRequirement", "PR_339")]
    [Trait("ModuleRequirement", "MR_3761")]
    [Fact]
    public void Validate_Should_Pass_When_CompareTypeIsNotBetween()
    {
        // Arrange
        var model = new ReflexCondition
        {
            CompareType = CompareType.Above,
            ResultType = ResultType.Concentration,
            ConcentrationCompareValue = 5.0,
            ConcentrationCompareValueUpperLimit = 3.0,
            TestName = "Test",
            MethodName = "Method",
            ReportingConcentration = true,
            ClassOptions = new List<string>(),
            CutOffOptions = new List<string>()
        };
        using var viewModel = new ReflexConditionViewModel(model, _methodTestDataSourceMock.Object, true);

        // Act
        var result = _sut.Validate(viewModel);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Should_Pass_When_ResultTypeIsNotConcentrationOrConcentrationRaw()
    {
        // Arrange
        var model = new ReflexCondition
        {
            CompareType = CompareType.EqualTo,
            ResultType = ResultType.Class,
            ConcentrationCompareValue = 5.0,
            ConcentrationCompareValueUpperLimit = 3.0,
            ClassCompareValue = "Positive",
            TestName = "Test",
            MethodName = "Method",
            ReportingClass = true,
            ClassOptions = new List<string> { "Positive", "Negative" },
            CutOffOptions = new List<string>()
        };
        using var viewModel = new ReflexConditionViewModel(model, _methodTestDataSourceMock.Object, true);

        // Act
        var result = _sut.Validate(viewModel);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Should_Fail_When_LowerLimitIsNull_ForConcentration()
    {
        // Arrange
        var model = new ReflexCondition
        {
            CompareType = CompareType.Between,
            ResultType = ResultType.Concentration,
            ConcentrationCompareValue = null,
            ConcentrationCompareValueUpperLimit = 5.0,
            TestName = "Test",
            MethodName = "Method",
            ReportingConcentration = true,
            ClassOptions = new List<string>(),
            CutOffOptions = new List<string>()
        };
        using var viewModel = new ReflexConditionViewModel(model, _methodTestDataSourceMock.Object, true);

        // Act
        var result = _sut.Validate(viewModel);

        // Assert
        result.IsValid.Should().BeFalse(); // Should fail because lower limit is required in Between scenario
        result.Errors.Should().Contain(e => e.ErrorMessage == "Minimum value is required");
    }

    [Fact]
    public void Validate_Should_Fail_When_UpperLimitIsNull_ForConcentration()
    {
        // Arrange
        var model = new ReflexCondition
        {
            CompareType = CompareType.Between,
            ResultType = ResultType.Concentration,
            ConcentrationCompareValue = 1.0,
            ConcentrationCompareValueUpperLimit = null,
            TestName = "Test",
            MethodName = "Method",
            ReportingConcentration = true,
            ClassOptions = new List<string>(),
            CutOffOptions = new List<string>()
        };
        using var viewModel = new ReflexConditionViewModel(model, _methodTestDataSourceMock.Object, true);

        // Act
        var result = _sut.Validate(viewModel);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Maximum value is required");
    }

    [Fact]
    public void Validate_Should_Pass_When_RuCompareValueIsSet_ForRU()
    {
        // Arrange
        var model = new ReflexCondition
        {
            CompareType = CompareType.Below,
            ResultType = ResultType.RU,
            RuCompareValue = 6.5,
            TestName = "Test",
            MethodName = "Method",
            ReportingConcentration = true,
            ClassOptions = new List<string>(),
            CutOffOptions = new List<string>()
        };
        using var viewModel = new ReflexConditionViewModel(model, _methodTestDataSourceMock.Object, true);

        // Act
        var result = _sut.Validate(viewModel);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Should_Fail_When_RuCompareValueIsOutOfRange_ForRU()
    {
        // Arrange
        var model_low = new ReflexCondition
        {
            CompareType = CompareType.Below,
            ResultType = ResultType.RU,
            RuCompareValue = 5.0,
            TestName = "Test",
            MethodName = "Method",
            ReportingConcentration = true,
            ClassOptions = new List<string>(),
            CutOffOptions = new List<string>()
        };

        var model_high = new ReflexCondition
        {
            CompareType = CompareType.Below,
            ResultType = ResultType.RU,
            RuCompareValue = 100.5,
            TestName = "Test",
            MethodName = "Method",
            ReportingConcentration = true,
            ClassOptions = new List<string>(),
            CutOffOptions = new List<string>()
        };

        using var viewModel_low = new ReflexConditionViewModel(model_low, _methodTestDataSourceMock.Object, true);
        using var viewModel_high = new ReflexConditionViewModel(model_high, _methodTestDataSourceMock.Object, true);

        // Act
        var result_low = _sut.Validate(viewModel_low);
        var result_high = _sut.Validate(viewModel_high);

        // Assert
        result_low.IsValid.Should().BeFalse();
        result_low.Errors.Should().Contain(e => e.ErrorMessage == "Value must be between 6 and 100");

        result_high.IsValid.Should().BeFalse();
        result_high.Errors.Should().Contain(e => e.ErrorMessage == "Value must be between 6 and 100");
    }

    [Fact]
    public void Validate_Should_Fail_When_RuCompareValueIsNull_ForRU()
    {
        // Arrange
        var model = new ReflexCondition
        {
            CompareType = CompareType.Below,
            ResultType = ResultType.RU,
            RuCompareValue = null,
            TestName = "Test",
            MethodName = "Method",
            ReportingConcentration = true,
            ClassOptions = new List<string>(),
            CutOffOptions = new List<string>()
        };
        using var viewModel = new ReflexConditionViewModel(model, _methodTestDataSourceMock.Object, true);

        // Act
        var result = _sut.Validate(viewModel);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "RU value is required");
    }
}
