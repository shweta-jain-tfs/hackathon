using FluentValidation.TestHelper;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Domain.Parameters.Methods;

namespace Pharos.Server.Parameters.Application.Commands.MethodTestDetails;

public class UpdateMethodTestDetailsCommandValidatorTests
{
    private readonly UpdateMethodTestDetailsCommandValidator _sut = new();

    [Fact]
    public void Validator_WhenLaboratoryNameIsTenCharactersLong_Should_NotReturnValidationError()
    {
        var command = CreateCommand([new MethodTestLabNameDto { TestName = "ABCDEFGHIJ", SortOrder = 1 }]);

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor("MethodTestLabNames[0].TestName");
    }

    [Trait("ProductRequirement", "PR_2588")]
    [Trait("ModuleRequirement", "MR_5959")]
    [Fact]
    public void Validator_WhenLaboratoryNameIsLongerThanTenCharacters_Should_ReturnValidationError()
    {
        var command = CreateCommand([new MethodTestLabNameDto { TestName = "ABCDEFGHIJK", SortOrder = 1 }]);

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor("MethodTestLabNames[0].TestName");
    }

    [Fact]
    public void Validator_WhenReportCutOffEnabledAndConcentrationMissing_ShouldReturnValidationError()
    {
        var command = CreateCommand();
        command.ReportCutOff = true;
        command.CutOffConcentration = null;

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.CutOffConcentration);
    }

    [Trait("ProductRequirement", "PR_348")]
    [Trait("ModuleRequirement", "MR_5956")]
    [Theory]
    [InlineData(-0.01)]
    [InlineData(100000)]
    public void Validator_WhenCutOffConcentrationIsOutsideRange_ShouldReturnValidationError(double value)
    {
        var command = CreateCommand();
        command.CutOffConcentration = value;

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.CutOffConcentration);
    }

    [Fact]
    public void Validator_WhenNoLaboratoryNamesRemain_Should_ReturnValidationError()
    {
        var command = CreateCommand([]);

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.MethodTestLabNames);
    }

    [Trait("ProductRequirement", "PR_348")]
    [Trait("ModuleRequirement", "MR_5105")]
    [Fact]
    public void Validator_WhenLaboratoryNamesAreDuplicated_Should_ReturnValidationError()
    {
        var command = CreateCommand(
        [
            new MethodTestLabNameDto { TestName = "A", SortOrder = 1 },
            new MethodTestLabNameDto { TestName = "a", SortOrder = 2 }
        ]);

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.MethodTestLabNames);
    }

    [Trait("ProductRequirement", "PR_348")]
    [Trait("ModuleRequirement", "MR_5105")]
    [Fact]
    public void Validator_WhenLaboratoryNameSortOrdersAreDuplicated_Should_ReturnValidationError()
    {
        var command = CreateCommand(
        [
            new MethodTestLabNameDto { TestName = "A", SortOrder = 1 },
            new MethodTestLabNameDto { TestName = "B", SortOrder = 1 }
        ]);

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.MethodTestLabNames);
    }

    private static UpdateMethodTestDetailsCommand CreateCommand(List<MethodTestLabNameDto> labNames)
    {
        return new UpdateMethodTestDetailsCommand
        {
            Id = 1,
            QueriedAt = DateTimeOffset.UtcNow,
            MethodTestLabNames = labNames
        };
    }

    [Trait("ProductRequirement", "PR_2588")]
    [Trait("ModuleRequirement", "MR_5957")]
    [Fact]
    public void Validator_WhenReportTextIsTooLong_ShouldReturnValidationError()
    {
        var command = CreateCommand();
        command.TextQuantitativeAbove = new string('A', MethodTest.TextQuantitativeResultLength + 1);

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.TextQuantitativeAbove);
    }

    [Fact]
    public void Validator_WhenReportClassDisabledAndClassIdSelected_ShouldReturnValidationError()
    {
        var command = CreateCommand();
        command.ReportClass = false;
        command.ReportClassId = 1;

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ReportClassId);
    }

    [Fact]
    public void Validator_WhenReportClassEnabledAndClassIdNotSelected_ShouldReturnValidationError()
    {
        var command = CreateCommand();
        command.ReportClass = true;
        command.ReportClassId = 0;

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ReportClassId);
    }

    private static UpdateMethodTestDetailsCommand CreateCommand()
    {
        return new UpdateMethodTestDetailsCommand
        {
            Id = 1,
            QueriedAt = DateTimeOffset.UtcNow,
            MethodConcentrationUnitId = 1,
            MethodTestLabNames = []
        };
    }
}
