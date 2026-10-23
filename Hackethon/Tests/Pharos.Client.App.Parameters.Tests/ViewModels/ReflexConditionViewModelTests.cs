using FluentAssertions;
using Moq;
using Pharos.Client.App.Parameters.Common.Models;
using Pharos.Client.App.Parameters.Reflex.ViewModels;
using Pharos.Client.Common.Models;
using Xunit;

namespace Pharos.Client.App.Parameters.Tests.ViewModels;

public class ReflexConditionViewModelTests
{
    private readonly Mock<IMethodTestDataSource> _methodTestDataSourceMock;

    public ReflexConditionViewModelTests()
    {
        _methodTestDataSourceMock = new Mock<IMethodTestDataSource>();
    }

    [Trait("ProductRequirement", "PR_339")]
    [Trait("ModuleRequirement", "MR_5951")]
    [Fact]
    public void ResultOptions_Should_ContainConcentrationOptions_When_ReportingConcentrationIsTrue()
    {
        // Arrange
        var model = new ReflexCondition
        {
            TestName = "",
            MethodName = "",
            ClassOptions = new List<string>(),
            CutOffOptions = new List<string>(),
            ReportingConcentration = true,
            ReportingClass = false,
            ReportingCutOff = false,
            ReportingQuotient = false
        };

        using var viewModel = new ReflexConditionViewModel(model, _methodTestDataSourceMock.Object, true);

        // Act
        viewModel.AddResultTypeOptions();

        // Assert
        viewModel.ResultOptions.Should().HaveCount(3); // Concentration, ConcentrationRaw, and RU
        viewModel.ResultOptions.Should().Contain(o => o.Value == ResultType.Concentration);
        viewModel.ResultOptions.Should().Contain(o => o.Value == ResultType.ConcentrationRaw);
        viewModel.ResultOptions.Should().Contain(o => o.Value == ResultType.RU);
    }

    [Trait("ProductRequirement", "PR_339")]
    [Trait("ModuleRequirement", "MR_5951")]
    [Fact]
    public void ResultOptions_Should_ContainClassOption_When_ReportingClassIsTrue()
    {
        // Arrange
        var model = new ReflexCondition
        {
            TestName = "",
            MethodName = "",
            ClassOptions = new List<string> { "0", "1", "2" },
            CutOffOptions = new List<string>(),
            ReportingConcentration = false,
            ReportingClass = true,
            ReportingCutOff = false,
            ReportingQuotient = false
        };

        using var viewModel = new ReflexConditionViewModel(model, _methodTestDataSourceMock.Object, true);

        // Act
        viewModel.AddResultTypeOptions();

        // Assert
        viewModel.ResultOptions.Should().Contain(o => o.Value == ResultType.Class);
        viewModel.ClassDropDownOptions.Should().HaveCount(3);
    }

    [Trait("ProductRequirement", "PR_339")]
    [Trait("ModuleRequirement", "MR_5951")]
    [Fact]
    public void ResultOptions_Should_ContainCutOffOption_When_ReportingCutOffIsTrue()
    {
        // Arrange
        var model = new ReflexCondition
        {
            TestName = "",
            MethodName = "",
            ClassOptions = new List<string>(),
            CutOffOptions = new List<string> { "0.35", "0.70", "3.50" },
            ReportingConcentration = false,
            ReportingClass = false,
            ReportingCutOff = true,
            ReportingQuotient = false
        };

        using var viewModel = new ReflexConditionViewModel(model, _methodTestDataSourceMock.Object, true);

        // Act
        viewModel.AddResultTypeOptions();

        // Assert
        viewModel.ResultOptions.Should().Contain(o => o.Value == ResultType.CutOff);
        viewModel.CutoffDropDownOptions.Should().HaveCount(3);
    }

    [Trait("ProductRequirement", "PR_348")]
    [Trait("ModuleRequirement", "MR_5956")]
    [Fact]
    public void ResultOptions_Should_ContainQuotientOption_When_ReportingQuotientIsTrue()
    {
        // Arrange
        var model = new ReflexCondition
        {
            TestName = "",
            MethodName = "",
            ClassOptions = new List<string>(),
            CutOffOptions = new List<string>(),
            ReportingConcentration = false,
            ReportingClass = false,
            ReportingCutOff = false,
            ReportingQuotient = true
        };

        using var viewModel = new ReflexConditionViewModel(model, _methodTestDataSourceMock.Object, true);

        // Act
        viewModel.AddResultTypeOptions();

        // Assert
        viewModel.ResultOptions.Should().Contain(o => o.Value == ResultType.Quotient);
    }

    [Trait("ProductRequirement", "PR_2588")]
    [Trait("ModuleRequirement", "MR_5957")]
    [Fact]
    public void CompareOptions_Should_ContainAboveBelowBetween_When_ResultTypeIsConcentration()
    {
        // Arrange
        var model = CreateBasicReflexCondition();
        using var viewModel = new ReflexConditionViewModel(model, _methodTestDataSourceMock.Object, true);

        // Act
        viewModel.AddCompareTypeOptions(ResultType.Concentration);

        // Assert
        viewModel.CompareOptions.Should().HaveCount(3);
        viewModel.CompareOptions.Should().Contain(o => o.Value == CompareType.Above);
        viewModel.CompareOptions.Should().Contain(o => o.Value == CompareType.Below);
        viewModel.CompareOptions.Should().Contain(o => o.Value == CompareType.Between);
    }

    [Fact]
    public void CompareOptions_Should_ContainOnlyEqualTo_When_ResultTypeIsClass()
    {
        // Arrange
        var model = CreateBasicReflexCondition();
        using var viewModel = new ReflexConditionViewModel(model, _methodTestDataSourceMock.Object, true);

        // Act
        viewModel.AddCompareTypeOptions(ResultType.Class);

        // Assert
        viewModel.CompareOptions.Should().HaveCount(1);
        viewModel.CompareOptions.Should().Contain(o => o.Value == CompareType.EqualTo);
    }

    [Trait("ProductRequirement", "PR_348")]
    [Trait("ModuleRequirement", "MR_5956")]
    [Fact]
    public void CompareOptions_Should_ContainAboveBelowBetween_When_ResultTypeIsQuotient()
    {
        // Arrange
        var model = CreateBasicReflexCondition();
        using var viewModel = new ReflexConditionViewModel(model, _methodTestDataSourceMock.Object, true);

        // Act
        viewModel.AddCompareTypeOptions(ResultType.Quotient);

        // Assert
        viewModel.CompareOptions.Should().HaveCount(3);
        viewModel.CompareOptions.Should().Contain(o => o.Value == CompareType.Above);
        viewModel.CompareOptions.Should().Contain(o => o.Value == CompareType.Below);
        viewModel.CompareOptions.Should().Contain(o => o.Value == CompareType.Between);
    }

    [Trait("ProductRequirement", "PR_339")]
    [Trait("ModuleRequirement", "MR_5951")]
    [Fact]
    public void ResetCompareType_Should_ClearAllCompareValues()
    {
        // Arrange
        var model = new ReflexCondition
        {
            TestName = "",
            MethodName = "",
            ClassOptions = new List<string>(),
            CutOffOptions = new List<string>(),
            ConcentrationCompareValue = 5.0,
            ConcentrationRawCompareValue = 100.0,
            ClassCompareValue = "2",
            QuotientCompareValue = 1.5,
            QuotientCompareValueUpperLimit = 2.5,
            CutOffCompareValue = "0.35",
            RuCompareValue = 0.75
        };

        using var viewModel = new ReflexConditionViewModel(model, _methodTestDataSourceMock.Object, true);

        // Act
        viewModel.ResetCompareType();

        // Assert
        viewModel.ConcentrationCompareValue.Should().BeNull();
        viewModel.ConcentrationRawCompareValue.Should().BeNull();
        viewModel.ClassCompareValue.Should().BeNull();
        viewModel.QuotientCompareValue.Should().BeNull();
        viewModel.CutOffCompareValue.Should().BeNull();
        viewModel.RuCompareValue.Should().BeNull();
    }

    [Fact]
    public void ResetUpperLimitValues_Should_ClearUpperLimitValues()
    {
        // Arrange
        var model = new ReflexCondition
        {
            TestName = "",
            MethodName = "",
            ClassOptions = new List<string>(),
            CutOffOptions = new List<string>(),
            ConcentrationCompareValueUpperLimit = 10.0,
            ConcentrationRawCompareValueUpperLimit = 500.0
        };

        using var viewModel = new ReflexConditionViewModel(model, _methodTestDataSourceMock.Object, true);

        // Act
        viewModel.ResetUpperLimitValues();

        // Assert
        viewModel.ConcentrationCompareValueUpperLimit.Should().BeNull();
        viewModel.ConcentrationRawCompareValueUpperLimit.Should().BeNull();
    }

    [Trait("ProductRequirement", "PR_339")]
    [Trait("ModuleRequirement", "MR_3760")]
    [Fact]
    public void IsCompleteCondition_Should_ReturnTrue_When_ConcentrationValueIsSet()
    {
        // Arrange
        var model = new ReflexCondition
        {
            TestName = "",
            MethodName = "",
            ClassOptions = new List<string>(),
            CutOffOptions = new List<string>(),
            ResultType = ResultType.Concentration,
            ConcentrationCompareValue = 5.0
        };

        using var viewModel = new ReflexConditionViewModel(model, _methodTestDataSourceMock.Object, true);

        // Act & Assert
        viewModel.IsCompleteCondition.Should().BeTrue();
    }

    [Trait("ProductRequirement", "PR_339")]
    [Trait("ModuleRequirement", "MR_3760")]
    [Fact]
    public void IsCompleteCondition_Should_ReturnFalse_When_ConcentrationValueIsNotSet()
    {
        // Arrange
        var model = new ReflexCondition
        {
            TestName = "",
            MethodName = "",
            ClassOptions = new List<string>(),
            CutOffOptions = new List<string>(),
            ResultType = ResultType.Concentration,
            ConcentrationCompareValue = null
        };

        using var viewModel = new ReflexConditionViewModel(model, _methodTestDataSourceMock.Object, true);

        // Act & Assert
        viewModel.IsCompleteCondition.Should().BeFalse();
    }

    [Trait("ProductRequirement", "PR_339")]
    [Trait("ModuleRequirement", "MR_3760")]
    [Fact]
    public void IsCompleteCondition_Should_ReturnTrue_When_ClassValueIsSet()
    {
        // Arrange
        var model = new ReflexCondition
        {
            TestName = "",
            MethodName = "",
            ClassOptions = new List<string>(),
            CutOffOptions = new List<string>(),
            ResultType = ResultType.Class,
            ClassCompareValue = "2"
        };

        using var viewModel = new ReflexConditionViewModel(model, _methodTestDataSourceMock.Object, true);

        // Act & Assert
        viewModel.IsCompleteCondition.Should().BeTrue();
    }

    [Trait("ProductRequirement", "PR_339")]
    [Trait("ModuleRequirement", "MR_5951")]
    [Fact]
    public void CheckIfDataIsValid_Should_ResetResult_When_ResultTypeNotReportedForTest()
    {
        // Arrange
        var model = new ReflexCondition
        {
            TestName = "Test",
            MethodName = "Method",
            ClassOptions = new List<string>(),
            CutOffOptions = new List<string>(),
            ResultType = ResultType.Class,
            ReportingClass = false, // Class not reported but selected as ResultType
            ReportingConcentration = true
        };

        using var viewModel = new ReflexConditionViewModel(model, _methodTestDataSourceMock.Object, true);

        // Act
        viewModel.CheckIfDataIsValid();

        // Assert
        viewModel.ResultType.Should().Be(ResultType.Default);
    }

    [Trait("ProductRequirement", "PR_339")]
    [Trait("ModuleRequirement", "MR_5951")]
    [Fact]
    public void ResultOptions_Should_ContainRuOption_When_ReportingConcentrationIsFalse()
    {
        // Arrange
        var model = new ReflexCondition
        {
            TestName = "",
            MethodName = "",
            ClassOptions = new List<string>(),
            CutOffOptions = new List<string>(),
            ReportingConcentration = false,
            ReportingClass = false,
            ReportingCutOff = false,
            ReportingQuotient = false
        };

        using var viewModel = new ReflexConditionViewModel(model, _methodTestDataSourceMock.Object, true);

        // Act
        viewModel.AddResultTypeOptions();

        // Assert
        viewModel.ResultOptions.Should().Contain(o => o.Value == ResultType.RU);
    }

    [Fact]
    public void CompareOptions_Should_ContainOnlyBelow_When_ResultTypeIsRU()
    {
        // Arrange
        var model = CreateBasicReflexCondition();
        using var viewModel = new ReflexConditionViewModel(model, _methodTestDataSourceMock.Object, true);

        // Act
        viewModel.AddCompareTypeOptions(ResultType.RU);

        // Assert
        viewModel.CompareOptions.Should().HaveCount(1);
        viewModel.CompareOptions.Should().Contain(o => o.Value == CompareType.Below);
    }

    [Fact]
    public void IsCompleteCondition_Should_ReturnTrue_When_RuCompareValueIsSet()
    {
        // Arrange
        var model = new ReflexCondition
        {
            TestName = "",
            MethodName = "",
            ClassOptions = new List<string>(),
            CutOffOptions = new List<string>(),
            ResultType = ResultType.RU,
            RuCompareValue = 1.5
        };

        using var viewModel = new ReflexConditionViewModel(model, _methodTestDataSourceMock.Object, true);

        // Act & Assert
        viewModel.IsCompleteCondition.Should().BeTrue();
    }

    [Fact]
    public void IsCompleteCondition_Should_ReturnFalse_When_RuCompareValueIsNotSet()
    {
        // Arrange
        var model = new ReflexCondition
        {
            TestName = "",
            MethodName = "",
            ClassOptions = new List<string>(),
            CutOffOptions = new List<string>(),
            ResultType = ResultType.RU,
            RuCompareValue = null
        };

        using var viewModel = new ReflexConditionViewModel(model, _methodTestDataSourceMock.Object, true);

        // Act & Assert
        viewModel.IsCompleteCondition.Should().BeFalse();
    }

    [Fact]
    public void CheckIfDataIsValid_Should_NotResetResult_When_ResultTypeIsRU()
    {
        // Arrange
        var model = new ReflexCondition
        {
            TestName = "Test",
            MethodName = "Method",
            ClassOptions = new List<string>(),
            CutOffOptions = new List<string>(),
            ResultType = ResultType.RU,
            ReportingConcentration = false,
            ReportingClass = false,
            ReportingCutOff = false,
            ReportingQuotient = false
        };

        using var viewModel = new ReflexConditionViewModel(model, _methodTestDataSourceMock.Object, true);

        // Act
        viewModel.CheckIfDataIsValid();

        // Assert
        viewModel.ResultType.Should().Be(ResultType.RU);
    }

    private static ReflexCondition CreateBasicReflexCondition()
    {
        return new ReflexCondition
        {
            TestName = "",
            MethodName = "",
            ClassOptions = new List<string>(),
            CutOffOptions = new List<string>()
        };
    }
}
