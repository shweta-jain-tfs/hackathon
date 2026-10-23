using Moq.EntityFrameworkCore;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Parameters.Application.Queries.GetMethods;

namespace Pharos.Server.Parameters.Application.Commands;

public class UpdateMethodCommandTests
{
    private readonly Mock<IParametersDbContext> _mockContext;
    private readonly UpdateMethodCommandHandler _handler;

    public UpdateMethodCommandTests()
    {
        _mockContext = new Mock<IParametersDbContext>();
        _mockContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new UpdateMethodCommandHandler(_mockContext.Object);
    }

    [Trait("ProductRequirement", "PR_2587")]
    [Trait("ModuleRequirement", "MR_5964")]
    [Fact]
    public async Task Handle_ReportSettingsChanged_UpdatesUserReportSettings()
    {
        // Arrange
        var method = CreateMethod();
        var calibrationCurve = CreateCalibrationCurve(method.Id);
        SetupMethods(method);
        SetupCalibrationCurves(calibrationCurve);

        var command = CreateCommand(method.Id);

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        method.Reporting.UserMinConcentrationToReport.Should().Be(command.Report!.MinConcentrationToReport);
        method.Reporting.UserTextBelowCutOff.Should().Be(command.Report.TextBelowCutOff);
        method.Reporting.UserTextAboveCutOff.Should().Be(command.Report.TextAboveCutOff);
        method.Reporting.UserTextQuantitativeBelow.Should().Be(command.Report.TextQuantitativeBelow);
        method.Reporting.UserTextQuantitativeDoubtful.Should().Be(command.Report.TextQuantitativeDoubtful);
        method.Reporting.UserTextQuantitativeAbove.Should().Be(command.Report.TextQuantitativeAbove);
        method.Reporting.UserUseCalculatedTextQuantitativeAbove.Should().Be(command.Report.UseCalculatedTextQuantitativeAbove);

        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Trait("ProductRequirement", "PR_1417")]
    [Trait("ModuleRequirement", "MR_5964")]
    [Fact]
    public async Task Handle_MaxNumberOfDaysChanged_UpdatesUserMaxNumberOfDays()
    {
        // Arrange
        var method = CreateMethod();
        var calibrationCurve = CreateCalibrationCurve(method.Id, userMaxNumberOfDays: 5);
        SetupMethods(method);
        SetupCalibrationCurves(calibrationCurve);

        var command = CreateCommand(method.Id, maxNumberOfDays: 10);

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        calibrationCurve.UserMaxNumberOfDays.Should().Be(10);

        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Trait("ProductRequirement", "PR_2588")]
    [Trait("ModuleRequirement", "MR_5964")]
    [Fact]
    public async Task Handle_MaxNumberOfDaysNotChanged_DoesNotSaveChanges()
    {
        // Arrange
        var method = CreateMethod();
        var calibrationCurve = CreateCalibrationCurve(method.Id, userMaxNumberOfDays: 7);
        SetupMethods(method);
        SetupCalibrationCurves(calibrationCurve);

        var command = CreateCommand(method.Id, maxNumberOfDays: 7);
        method.Reporting.UserMinConcentrationToReport = command.Report!.MinConcentrationToReport;
        method.Reporting.UserTextBelowCutOff = command.Report.TextBelowCutOff;
        method.Reporting.UserTextAboveCutOff = command.Report.TextAboveCutOff;
        method.Reporting.UserTextQuantitativeBelow = command.Report.TextQuantitativeBelow;
        method.Reporting.UserTextQuantitativeDoubtful = command.Report.TextQuantitativeDoubtful;
        method.Reporting.UserTextQuantitativeAbove = command.Report.TextQuantitativeAbove;
        method.Reporting.UserUseCalculatedTextQuantitativeAbove = command.Report.UseCalculatedTextQuantitativeAbove;
        method.MethodNameLis = command.MethodNameLis;
        method.DefaultLisChannelId = command.DefaultLisChannelId;
        var commandClassTable = command.ClassTables!.Single();
        method.ClassTableBelows.Single().UserTextBelowConcentration = commandClassTable.TextBelow;
        method.ClassTables.Single().UserTextAboveConcentration = commandClassTable.ClassTableRow!.Single().TextAbove;

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ClassTableDetailsChanged_UpdatesUserClassTableText()
    {
        // Arrange
        var method = CreateMethod();
        SetupMethods(method);
        SetupCalibrationCurves();

        var command = CreateCommand(method.Id);

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var commandClassTable = command.ClassTables!.Single();
        method.ClassTableBelows.Single().UserTextBelowConcentration.Should().Be(commandClassTable.TextBelow);
        method.ClassTables.Single().UserTextAboveConcentration.Should().Be(commandClassTable.ClassTableRow!.Single().TextAbove);

        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EnabledChanged_AddsMethodEnabledEvent()
    {
        // Arrange
        var method = CreateMethod(enabled: false);
        SetupMethods(method);
        SetupCalibrationCurves(CreateCalibrationCurve(method.Id));

        var command = CreateCommand(method.Id, enabled: true);

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        method.Enabled.Should().BeTrue();
        method.DomainEvents.Should().ContainSingle()
            .Which.Should().Be(new MethodEnabledEvent(method.MethodName, true));

        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MethodNameLisChanged_UpdatesMethodNameLis()
    {
        // Arrange
        var method = CreateMethod();
        var command = CreateCommand(method.Id);
        var calibrationCurve = CreateCalibrationCurve(method.Id, userMaxNumberOfDays: command.MaxNumberOfDays);
        method.DefaultLisChannelId = command.DefaultLisChannelId;
        SetExistingReportSettings(method, command.Report!);
        SetupMethods(method);
        SetupCalibrationCurves(calibrationCurve);

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        method.MethodNameLis.Should().Be(command.MethodNameLis);
        method.DomainEvents.Should().BeEmpty();

        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DefaultLisChannelIdChanged_UpdatesDefaultLisChannelId()
    {
        // Arrange
        var method = CreateMethod();
        var command = CreateCommand(method.Id);
        var calibrationCurve = CreateCalibrationCurve(method.Id, userMaxNumberOfDays: command.MaxNumberOfDays);
        method.MethodNameLis = command.MethodNameLis;
        SetExistingReportSettings(method, command.Report!);
        SetupMethods(method);
        SetupCalibrationCurves(calibrationCurve);

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        method.DefaultLisChannelId.Should().Be(command.DefaultLisChannelId);
        method.DomainEvents.Should().BeEmpty();

        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EnabledNotChanged_DoesNotAddMethodEnabledEvent()
    {
        // Arrange
        var method = CreateMethod(enabled: true);
        SetupMethods(method);
        SetupCalibrationCurves(CreateCalibrationCurve(method.Id));

        var command = CreateCommand(method.Id, enabled: true);

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        method.Enabled.Should().BeTrue();
        method.DomainEvents.Should().BeEmpty();

        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoChanges_DoesNotSaveChanges()
    {
        // Arrange
        var method = CreateMethod();
        var command = CreateCommand(method.Id, maxNumberOfDays: 7);
        var calibrationCurve = CreateCalibrationCurve(method.Id, userMaxNumberOfDays: 7);
        method.Reporting.UserMinConcentrationToReport = command.Report!.MinConcentrationToReport;
        method.Reporting.UserTextBelowCutOff = command.Report.TextBelowCutOff;
        method.Reporting.UserTextAboveCutOff = command.Report.TextAboveCutOff;
        method.Reporting.UserTextQuantitativeBelow = command.Report.TextQuantitativeBelow;
        method.Reporting.UserTextQuantitativeDoubtful = command.Report.TextQuantitativeDoubtful;
        method.Reporting.UserTextQuantitativeAbove = command.Report.TextQuantitativeAbove;
        method.Reporting.UserUseCalculatedTextQuantitativeAbove = command.Report.UseCalculatedTextQuantitativeAbove;
        method.MethodNameLis = command.MethodNameLis;
        method.DefaultLisChannelId = command.DefaultLisChannelId;
        var commandClassTable = command.ClassTables!.Single();
        method.ClassTableBelows.Single().UserTextBelowConcentration = commandClassTable.TextBelow;
        method.ClassTables.Single().UserTextAboveConcentration = commandClassTable.ClassTableRow!.Single().TextAbove;
        SetupMethods(method);
        SetupCalibrationCurves(calibrationCurve);

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        method.DomainEvents.Should().BeEmpty();

        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_MethodNotFound_ThrowsObjectGoneException()
    {
        // Arrange
        SetupMethods();
        SetupCalibrationCurves();

        var command = CreateCommand(methodId: 1);

        // Act & Assert
        await Assert.ThrowsAsync<ObjectGoneException>(() => _handler.Handle(command, TestContext.Current.CancellationToken));

        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private void SetupMethods(params Method[] methods)
    {
        _mockContext.Setup(x => x.Methods).ReturnsDbSet(methods);
    }

    private void SetupCalibrationCurves(params MethodCalibrationCurve[] calibrationCurves)
    {
        _mockContext.Setup(x => x.MethodCalibrationCurves).ReturnsDbSet(calibrationCurves);
    }

    private static Method CreateMethod(bool enabled = true)
    {
        return new Method
        {
            Id = 1,
            MethodName = "sIgE",
            Enabled = enabled,
            MethodNameLis = "methodNameLis",
            DefaultLisChannelId = 1,
            Reporting = new MethodReporting(),
            ClassTableBelows =
            [
                new MethodClassTableBelow
                {
                    ClassTableId = 1,
                    DefaultTextBelowConcentration = "0"
                }
            ],
            ClassTables =
            [
                new MethodClassTable
                {
                    ClassTableId = 1,
                    ClassTableRowId = 1,
                    Concentration = 0.35,
                    DefaultTextAboveConcentration = "1"
                }
            ]
        };
    }

    private static MethodCalibrationCurve CreateCalibrationCurve(int methodId, int? userMaxNumberOfDays = null)
    {
        return new MethodCalibrationCurve
        {
            MethodId = methodId,
            DefaultMaxNumberOfDays = 14,
            UserMaxNumberOfDays = userMaxNumberOfDays
        };
    }

    private static void SetExistingReportSettings(Method method, MethodReportingDto report)
    {
        method.Reporting.UserMinConcentrationToReport = report.MinConcentrationToReport;
        method.Reporting.UserTextBelowCutOff = report.TextBelowCutOff;
        method.Reporting.UserTextAboveCutOff = report.TextAboveCutOff;
        method.Reporting.UserTextQuantitativeBelow = report.TextQuantitativeBelow;
        method.Reporting.UserTextQuantitativeDoubtful = report.TextQuantitativeDoubtful;
        method.Reporting.UserTextQuantitativeAbove = report.TextQuantitativeAbove;
        method.Reporting.UserUseCalculatedTextQuantitativeAbove = report.UseCalculatedTextQuantitativeAbove;
    }

    private static UpdateMethodCommand CreateCommand(int methodId, bool enabled = true, int maxNumberOfDays = 7)
    {
        return new UpdateMethodCommand
        {
            Id = methodId,
            Enabled = enabled,
            MethodNameLis = "UpdatedMethodNameLis",
            DefaultLisChannelId = 2,
            MaxNumberOfDays = maxNumberOfDays,
            Report = new MethodReportingDto
            {
                MinConcentrationToReport = 12.34,
                TextBelowCutOff = "Below",
                TextAboveCutOff = "Above",
                TextQuantitativeBelow = "<0.1",
                TextQuantitativeDoubtful = "Doubt",
                TextQuantitativeAbove = ">100",
                UseCalculatedTextQuantitativeAbove = true
            },
            ClassTables =
            [
                new MethodClassTableDto
                {
                    ClassTableId = 1,
                    TextBelow = "Negative",
                    ClassTableRow =
                    [
                        new ClassTableRowInfoDto
                        {
                            ClassTableRowId = 1,
                            Concentration = 0.35,
                            TextAbove = "Positive"
                        }
                    ]
                }
            ]
        };
    }
}
