using FluentAssertions;
using Pharos.Client.App.Parameters.Methods.ViewModels;
using Pharos.Client.Common.Models;
using Xunit;

namespace Pharos.Client.App.Parameters.Tests.ViewModels;

public class MethodTestDetailsReportViewModelTests
{
    [Trait("ProductRequirement", "PR_2588")]
    [Trait("ModuleRequirement", "MR_5956")]
    [Fact]
    public void Constructor_ShouldCreateExpectedDropdownOptions()
    {
        var model = CreateModel();

        using var viewModel = new MethodTestDetailsReportViewModel(model);

        viewModel.ConcentrationUnitDropDownValues.Select(x => x.Value).Should().Equal(1, 2);
        viewModel.ReportClassIdDropDownValues.Select(x => x.Value).Should().Equal(0, 1);
        viewModel.ReportQuotientDropDownValues.Select(x => x.Value).Should().Equal(
            QuotientReportMode.NoReport,
            QuotientReportMode.ReportAsFraction,
            QuotientReportMode.ReportAsPercent);
    }

    [Trait("ProductRequirement", "PR_2588")]
    [Trait("ModuleRequirement", "MR_5957")]
    [Fact]
    public void ReportClass_WhenSetFalse_ShouldClearReportClassId()
    {
        var model = CreateModel();
        model.ReportClass = true;
        model.ReportClassId = 1;
        using var viewModel = new MethodTestDetailsReportViewModel(model);

        viewModel.ReportClass = false;

        viewModel.ReportClassId.Should().Be(0);
        viewModel.CanEditClassTable.Should().BeFalse();
    }

    [Trait("ProductRequirement", "PR_2588")]
    [Trait("ModuleRequirement", "MR_5957")]
    [Fact]
    public void ReportClassId_WhenSelected_ShouldEnableReportClass()
    {
        var model = CreateModel();
        model.ReportClass = false;
        model.ReportClassId = 0;
        using var viewModel = new MethodTestDetailsReportViewModel(model);

        viewModel.ReportClassId = 1;

        viewModel.ReportClass.Should().BeTrue();
        viewModel.CanEditClassTable.Should().BeTrue();
    }

    [Fact]
    public void ReportSettingChanged_ShouldMarkParentViewModelAsModified()
    {
        var model = CreateModel();
        using var viewModel = new MethodTestDetailsViewModel(model);

        viewModel.TestDetailsReportViewModel.ReportCutOff = true;

        viewModel.IsModified.Should().BeTrue();
    }

    private static MethodTestDetails CreateModel()
    {
        return new MethodTestDetails
        {
            ConcentrationUnits =
            [
                new MethodConcentrationUnitOption { UnitId = 1, ConcentrationUnit = "kU/L" },
                new MethodConcentrationUnitOption { UnitId = 2, ConcentrationUnit = "PAU/L" }
            ],
            ClassTables =
            [
                new MethodClassTableOption { ClassTableId = 1, DisplayName = "1" }
            ],
            MethodTestLabNames = []
        };
    }
}
