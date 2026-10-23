using MediatR;
using Microsoft.Extensions.Logging;
using Moq.EntityFrameworkCore;
using Pharos.Server.Application.Result;
using Pharos.Server.Application.Security;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Parameters.Application.Services;
using Pharos.Server.Result.Application.Commands.ProcessAssayResult;
using Tetris.Protocol.Pharos.TestResult;

namespace Pharos.Server.Result.Application.Commands;

public class ProcessAssayResultCommandHandlerTests
{
    private readonly ProcessAssayResultCommandHandler _sut;
    private readonly Mock<ILogger<ProcessAssayResultCommandHandler>> _loggerMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<IParametersDbContext> _parametersDbContextMock = new();

    public ProcessAssayResultCommandHandlerTests()
    {
        var methodsService = new MethodsService(_parametersDbContextMock.Object, new Mock<IAuthorizationService>().Object);
        _sut = new ProcessAssayResultCommandHandler(_mediatorMock.Object, methodsService, _loggerMock.Object);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_3538")]
    public async Task ViewAllergy_TreatCurveControlMessageAsCalibratorMessage()
    {
        PrepareMethodParametersForViewAllergy();

        var assayResultMessage = CreateViewAllergyAssayResultMessage();

        await _sut.Handle(new ProcessAssayResultCommand("SN0001", assayResultMessage), TestContext.Current.CancellationToken);

        var mediatorSends = _mediatorMock.Invocations.ToList();
        mediatorSends.Should().HaveCountGreaterOrEqualTo(1);
        mediatorSends[0].Arguments.Should().HaveCountGreaterOrEqualTo(1);

        var processAssayResultCommand = mediatorSends[0].Arguments[0] as BaseProcessAssayResultCommand;
        processAssayResultCommand.Should().NotBeNull();

        var processCalibratorResultCommand = processAssayResultCommand as ProcessCalibratorResultCommand;
        processCalibratorResultCommand.Should().NotBeNull();
    }

    /// <summary>
    /// Prepare method parameters
    /// </summary>
    private void PrepareMethodParametersForViewAllergy()
    {
        var methodTests = new List<MethodTest>()
        {
            new()
            {
                TestName = "Vt17",
                ConcentrationFactor = 1,
                UseLotSpecificCode = false,
                IterativeAlfaBeta1Beta2Flag = false,
                AdjustmentForAlfaBeta1Beta2 = 1,
                HighestConcentrationToReport = null,
                LowerDetectionLimit = 0,
                ReportConcentrationBelowCurveFlag = false,
                ReportConcentrationAboveCurveFlag = false,
                DefaultCutOffConcentration = 0.35,
                DefaultReportCutOff = false,
                DefaultReportConcentration = true,
                DefaultReportClassId = 0,
                DefaultReportQuotient = QuotientReportMode.ReportAsFraction,
                DefaultReplicates = 1,

                Method = new Method()
                {
                    MethodName = "View",
                    Technology = AssayTechnology.ViewAllergy,
                    Calculation = new MethodCalculation()
                    {
                        CalculationType = CalculationType.Quotient,
                    },
                    Reporting = new MethodReporting()
                    {
                        DefaultMinConcentrationToReport = 0,
                        DefaultTextBelowCutOff = "Negative",
                        DefaultTextAboveCutOff = "Positive",
                    },
                    CalibrationCurve = new MethodCalibrationCurve()
                    {
                        QuotientIndex = 1,
                    },
                    ClassTableBelows = new List<MethodClassTableBelow>()
                    {
                        new() { ClassTableId = 1, DefaultTextBelowConcentration ="<1000", },
                        new() { ClassTableId = 2, DefaultTextBelowConcentration = "<10", },
                    },
                    ClassTables = new List<MethodClassTable>()
                    {
                        new() { ClassTableId= 1, ClassTableRowId = 1, Concentration = 1000, DefaultTextAboveConcentration = ">1000" },
                        new() { ClassTableId= 2, ClassTableRowId = 1, Concentration = 10, DefaultTextAboveConcentration = ">10" },
                        new() { ClassTableId= 2, ClassTableRowId = 2, Concentration = 20, DefaultTextAboveConcentration = ">20" },
                        new() { ClassTableId= 2, ClassTableRowId = 3, Concentration = 30, DefaultTextAboveConcentration = ">30" },
                    },
                },
            }
        };
        _parametersDbContextMock.Setup(db => db.MethodTests).ReturnsDbSet(methodTests);

        var methods = methodTests.Select(x => x.Method).ToList();
        _parametersDbContextMock.Setup(db => db.Methods).ReturnsDbSet(methods);

        var methodCalibrators = methods.Select(x => x.CalibrationCurve).ToList();
        foreach (var mc in methodCalibrators)
        {
            mc.Method = methods.First(m => m.CalibrationCurve == mc);
        }
        _parametersDbContextMock.Setup(db => db.MethodCalibrationCurves).ReturnsDbSet(methodCalibrators);
    }

    private static AssayResult CreateViewAllergyAssayResultMessage()
    {
        return new AssayResult
        {
            MethodName = "View",
            TestType = EnumTestType.CurveControl
        };
    }
}
