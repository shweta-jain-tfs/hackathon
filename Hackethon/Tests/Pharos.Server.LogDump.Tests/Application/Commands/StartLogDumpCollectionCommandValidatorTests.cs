using FluentValidation.TestHelper;
using Pharos.Server.LogDump.Application.Commands.StartLogDump;

namespace Pharos.Server.LogDump.Tests.Application.Commands;

public class StartLogDumpCollectionCommandValidatorTests
{
    private readonly StartLogDumpCollectionCommandValidator _sut = new();

    [Trait("ProductRequirement", "PR_484")]
    [Fact]
    public void Validator_WhenSingleDateRequestIsValid_ShouldNotReturnValidationError()
    {
        var command = CreateCommand(new LogDumpCollectionRequest(
            LogCollectionDateMode.SingleDate,
            new DateOnly(2026, 8, 10),
            null,
            null,
            LogFileSelection.Default,
            LogFileSelection.None));

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Trait("ProductRequirement", "PR_484")]
    [Fact]
    public void Validator_WhenDateRangeRequestIsValid_ShouldNotReturnValidationError()
    {
        var command = CreateCommand(new LogDumpCollectionRequest(
            LogCollectionDateMode.DateRange,
            null,
            new DateOnly(2026, 8, 9),
            new DateOnly(2026, 8, 10),
            LogFileSelection.All,
            LogFileSelection.Default));

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Trait("ProductRequirement", "PR_484")]
    [Fact]
    public void Validator_WhenSingleDateModeHasNoSingleDate_ShouldReturnValidationError()
    {
        var command = CreateCommand(new LogDumpCollectionRequest(
            LogCollectionDateMode.SingleDate,
            null,
            null,
            null,
            LogFileSelection.Default,
            LogFileSelection.None));

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(command => command.Request.SingleDate);
    }

    [Trait("ProductRequirement", "PR_484")]
    [Fact]
    public void Validator_WhenDateRangeModeHasNoFromDate_ShouldReturnValidationError()
    {
        var command = CreateCommand(new LogDumpCollectionRequest(
            LogCollectionDateMode.DateRange,
            null,
            null,
            new DateOnly(2026, 8, 10),
            LogFileSelection.Default,
            LogFileSelection.None));

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(command => command.Request.FromDate);
    }

    [Trait("ProductRequirement", "PR_484")]
    [Fact]
    public void Validator_WhenDateRangeModeHasNoToDate_ShouldReturnValidationError()
    {
        var command = CreateCommand(new LogDumpCollectionRequest(
            LogCollectionDateMode.DateRange,
            null,
            new DateOnly(2026, 8, 9),
            null,
            LogFileSelection.Default,
            LogFileSelection.None));

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(command => command.Request.ToDate);
    }

    [Trait("ProductRequirement", "PR_484")]
    [Fact]
    public void Validator_WhenDateRangeIsReversed_ShouldReturnValidationError()
    {
        var command = CreateCommand(new LogDumpCollectionRequest(
            LogCollectionDateMode.DateRange,
            null,
            new DateOnly(2026, 8, 10),
            new DateOnly(2026, 8, 9),
            LogFileSelection.All,
            LogFileSelection.None));

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(command => command.Request);
    }

    [Trait("ProductRequirement", "PR_484")]
    [Theory]
    [InlineData(LogFileSelection.None)]
    [InlineData(LogFileSelection.Default)]
    [InlineData(LogFileSelection.All)]
    public void Validator_WhenPharosFilesSelectionIsValid_ShouldNotReturnValidationError(LogFileSelection selection)
    {
        var command = CreateCommand(new LogDumpCollectionRequest(
            LogCollectionDateMode.SingleDate,
            new DateOnly(2026, 8, 10),
            null,
            null,
            selection,
            LogFileSelection.None));

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(command => command.Request.PharosFiles);
    }

    [Trait("ProductRequirement", "PR_484")]
    [Theory]
    [InlineData(LogFileSelection.None)]
    [InlineData(LogFileSelection.Default)]
    [InlineData(LogFileSelection.All)]
    public void Validator_WhenInstrumentFilesSelectionIsValid_ShouldNotReturnValidationError(LogFileSelection selection)
    {
        var command = CreateCommand(new LogDumpCollectionRequest(
            LogCollectionDateMode.SingleDate,
            new DateOnly(2026, 8, 10),
            null,
            null,
            LogFileSelection.Default,
            selection));

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(command => command.Request.InstrumentFiles);
    }

    private static StartLogDumpCollectionCommand CreateCommand(LogDumpCollectionRequest request)
    {
        return new StartLogDumpCollectionCommand(request, LogDumpRequestSource.Pharos);
    }
}