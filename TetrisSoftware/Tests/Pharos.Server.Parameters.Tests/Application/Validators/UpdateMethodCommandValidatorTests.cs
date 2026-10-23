using FluentValidation.TestHelper;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Commands;
using Pharos.Server.Parameters.Application.Queries.GetMethods;

namespace Pharos.Server.Parameters.Application.Validators;

public class UpdateMethodCommandValidatorTests
{
    private readonly UpdateMethodCommandValidator _sut = new();

    [Fact]
    public void Validator_UpdateMethodCommand_ValidCommand()
    {
        var command = CreateCommand();

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public void Validator_UpdateMethodCommand_MethodId(int parameter, bool shouldPass)
    {
        var command = CreateCommand(methodId: parameter);

        var result = _sut.TestValidate(command);

        if (shouldPass)
        {
            result.ShouldNotHaveValidationErrorFor(x => x.Id);
        }
        else
        {
            result.ShouldHaveValidationErrorFor(x => x.Id);
        }
    }

    [Trait("ProductRequirement", "PR_339")]
    [Trait("ModuleRequirement", "MR_5963")]
    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(99999, true)]
    [InlineData(100000, false)]
    public void Validator_UpdateMethodCommand_MinConcentrationToReport(double parameter, bool shouldPass)
    {
        var command = CreateCommand(report: CreateReport(minConcentrationToReport: parameter));

        var result = _sut.TestValidate(command);

        if (shouldPass)
        {
            result.ShouldNotHaveValidationErrorFor("Report.MinConcentrationToReport");
        }
        else
        {
            result.ShouldHaveValidationErrorFor("Report.MinConcentrationToReport");
        }
    }

    [Trait("ProductRequirement", "PR_339")]
    [Trait("ModuleRequirement", "MR_5963")]
    [Theory]
    [InlineData(nameof(MethodReportingDto.TextBelowCutOff), MethodReporting.ClassResultTextLength, true)]
    [InlineData(nameof(MethodReportingDto.TextBelowCutOff), MethodReporting.ClassResultTextLength + 1, false)]
    [InlineData(nameof(MethodReportingDto.TextAboveCutOff), MethodReporting.ClassResultTextLength, true)]
    [InlineData(nameof(MethodReportingDto.TextAboveCutOff), MethodReporting.ClassResultTextLength + 1, false)]
    [InlineData(nameof(MethodReportingDto.TextQuantitativeBelow), MethodReporting.QuantitativeResultTextLength, true)]
    [InlineData(nameof(MethodReportingDto.TextQuantitativeBelow), MethodReporting.QuantitativeResultTextLength + 1, false)]
    [InlineData(nameof(MethodReportingDto.TextQuantitativeDoubtful), MethodReporting.QuantitativeResultTextLength, true)]
    [InlineData(nameof(MethodReportingDto.TextQuantitativeDoubtful), MethodReporting.QuantitativeResultTextLength + 1, false)]
    [InlineData(nameof(MethodReportingDto.TextQuantitativeAbove), MethodReporting.QuantitativeResultTextLength, true)]
    [InlineData(nameof(MethodReportingDto.TextQuantitativeAbove), MethodReporting.QuantitativeResultTextLength + 1, false)]
    public void Validator_UpdateMethodCommand_ReportTextLength(string propertyName, int length, bool shouldPass)
    {
        var command = CreateCommand(report: CreateReport(propertyName, new string('A', length)));

        var result = _sut.TestValidate(command);

        if (shouldPass)
        {
            result.ShouldNotHaveValidationErrorFor($"Report.{propertyName}");
        }
        else
        {
            result.ShouldHaveValidationErrorFor($"Report.{propertyName}");
        }
    }

    [Trait("ProductRequirement", "PR_339")]
    [Trait("ModuleRequirement", "MR_5963")]
    [Theory]
    [InlineData("", false)]
    [InlineData("12345678", true)]
    [InlineData("123456789", false)]
    public void Validator_UpdateMethodCommand_ClassTableText(string text, bool shouldPass)
    {
        var command = CreateCommand();
        var commandClassTable = command.ClassTables!.Single();
        commandClassTable.TextBelow = text;
        commandClassTable.ClassTableRow!.Single().TextAbove = text;

        var result = _sut.TestValidate(command);

        if (shouldPass)
        {
            result.ShouldNotHaveValidationErrorFor("ClassTables[0].TextBelow");
            result.ShouldNotHaveValidationErrorFor("ClassTables[0].ClassTableRow[0].TextAbove");
        }
        else
        {
            result.ShouldHaveValidationErrorFor("ClassTables[0].TextBelow");
            result.ShouldHaveValidationErrorFor("ClassTables[0].ClassTableRow[0].TextAbove");
        }
    }

    private static UpdateMethodCommand CreateCommand(
        int methodId = 1,
        MethodReportingDto? report = null)
    {
        return new UpdateMethodCommand
        {
            Id = methodId,
            Enabled = true,
            MethodNameLis = "TestMethod",
            DefaultLisChannelId = 1,
            Report = report ?? CreateReport(),
            ClassTables =
            [
                new MethodClassTableDto
                {
                    ClassTableId = 1,
                    TextBelow = "0",
                    ClassTableRow =
                    [
                        new ClassTableRowInfoDto
                        {
                            ClassTableRowId = 1,
                            Concentration = 0.35,
                            TextAbove = "1"
                        }
                    ]
                }
            ]
        };
    }

    private static MethodReportingDto CreateReport(
        string? propertyName = null,
        string? value = null,
        double minConcentrationToReport = 0)
    {
        return new MethodReportingDto
        {
            MinConcentrationToReport = minConcentrationToReport,
            TextBelowCutOff = propertyName == nameof(MethodReportingDto.TextBelowCutOff) ? value : "Below",
            TextAboveCutOff = propertyName == nameof(MethodReportingDto.TextAboveCutOff) ? value : "Above",
            TextQuantitativeBelow = propertyName == nameof(MethodReportingDto.TextQuantitativeBelow) ? value : "Below",
            TextQuantitativeDoubtful = propertyName == nameof(MethodReportingDto.TextQuantitativeDoubtful) ? value : "Doubt",
            TextQuantitativeAbove = propertyName == nameof(MethodReportingDto.TextQuantitativeAbove) ? value : "Above"
        };
    }
}
