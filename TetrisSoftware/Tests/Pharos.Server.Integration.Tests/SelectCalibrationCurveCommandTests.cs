using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Result.Application.Commands;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Application.Queries.GetCalibrationCurve;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Integration.Tests;

[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestsCollection))]
public class SelectCalibrationCurveCommandTests(IntegrationTestsWaf factory) : IntegrationTestBase(factory)
{
    const string _methodName = "sIgE";
    const string _conjugateLot = "conjL";

    [Fact]
    public async Task ChangeCalibratorCurve_Should_ResetCurveIssue_And_ChangeCalculationCurveÍd()
    {
        // Arrange
        await using (var asyncScope = Factory.Services.CreateAsyncScope())
        {
            var mediator = asyncScope.ServiceProvider.GetService<IMediator>()!;
            var dbContext = asyncScope.ServiceProvider.GetRequiredService<IResultDbContext>();
            var paramContext = asyncScope.ServiceProvider.GetRequiredService<IParametersDbContext>();

            var (_, instrumentState) = await Factory.AddInstrumentConnection();
            var serialNumber = instrumentState.SerialNumber;

            await AddCurve(dbContext, serialNumber);
            await AddAnalyticalRun(serialNumber, dbContext);
            await AddSampleTest(serialNumber, ResultApprovalStatus.NotApproved, TestResultStatus.PendingApproval, dbContext);
            await AddMethodParameters(paramContext);

            dbContext.SampleTestResults.Count().Should().Be(1);
            dbContext.SampleTestResults.First().MeanConcentration.Should().Be(1);
            dbContext.Curves.Count().Should().Be(2);
            dbContext.AnalyticalRuns.Count().Should().Be(1);

            var command = new ChangeCurveForAnalyticalRunCommand(2, 1);

            // Act
            await mediator.Send(command, TestContext.Current.CancellationToken);
        }

        await WaitForNotificationsAsync();

        // Assert
        var dbContext1 = ServiceProvider.GetRequiredService<IResultDbContext>();
        dbContext1.AnalyticalRuns.First().CurveId.Should().Be(2);
        dbContext1.AnalyticalRuns.First().CalibrationCurveIssue.Should().Be(AnalyticalRunIssue.NoIssue);
    }


    [Fact]
    public async Task GetAvailableCalibrationCurvesQuery_ShouldReturn_ValidCurves_NonRndMode()
    {
        //Arrange
        FakeAuthorizationService.RndModeEnabled = false;

        var mediator = ServiceProvider.GetService<IMediator>()!;
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var paramContext = ServiceProvider.GetRequiredService<IParametersDbContext>();

        var (_, instrumentState) = await Factory.AddInstrumentConnection();
        var serialNumber = instrumentState.SerialNumber;

        await AddCurveOptions(dbContext, serialNumber);
        await AddAnalyticalRun(serialNumber, dbContext);
        await AddSampleTest(serialNumber, ResultApprovalStatus.NotApproved, TestResultStatus.PendingApproval, dbContext);
        await AddMethodParameters(paramContext);

        dbContext.SampleTestResults.Count().Should().Be(1);
        dbContext.SampleTestResults.First().MeanConcentration.Should().Be(1);
        dbContext.Curves.Count().Should().Be(9);
        dbContext.AnalyticalRuns.Count().Should().Be(1);

        var query = new GetAvailableCalibrationCurvesQuery(1);

        //Act
        var result = await mediator.Send(query, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Count.Should().Be(3);
        result.All(x => x.Judgment == CurveJudgment.OK).Should().BeTrue();
    }

    [Fact]
    public async Task GetAvailableCalibrationCurvesQuery_ShouldReturn_OkAndFlaggedIncludingOldCurves_InRndMode()
    {
        //Arrange
        FakeAuthorizationService.RndModeEnabled = true;

        var mediator = ServiceProvider.GetService<IMediator>()!;
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var paramContext = ServiceProvider.GetRequiredService<IParametersDbContext>();

        var (_, instrumentState) = await Factory.AddInstrumentConnection();
        var serialNumber = instrumentState.SerialNumber;

        await AddCurveOptions(dbContext, serialNumber);
        await AddAnalyticalRun(serialNumber, dbContext);
        await AddSampleTest(serialNumber, ResultApprovalStatus.NotApproved, TestResultStatus.PendingApproval, dbContext);
        await AddMethodParameters(paramContext);

        var query = new GetAvailableCalibrationCurvesQuery(1);

        //Act
        var result = await mediator.Send(query, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Count.Should().Be(4);
        result.Any(x => x.Id == 6).Should().BeTrue();
        result.All(x => x.Judgment is CurveJudgment.OK or CurveJudgment.Flagged).Should().BeTrue();
    }

    [Fact]
    public async Task ChangeCalibratorCurve_ShouldAcceptFlaggedCurve_InRndMode()
    {
        // Arrange
        FakeAuthorizationService.RndModeEnabled = true;

        await using (var asyncScope = Factory.Services.CreateAsyncScope())
        {
            var mediator = asyncScope.ServiceProvider.GetService<IMediator>()!;
            var dbContext = asyncScope.ServiceProvider.GetRequiredService<IResultDbContext>();
            var paramContext = asyncScope.ServiceProvider.GetRequiredService<IParametersDbContext>();

            var (_, instrumentState) = await Factory.AddInstrumentConnection();
            var serialNumber = instrumentState.SerialNumber;

            await AddCurve(dbContext, serialNumber);
            await AddAnalyticalRun(serialNumber, dbContext);
            await AddSampleTest(serialNumber, ResultApprovalStatus.NotApproved, TestResultStatus.PendingApproval, dbContext);
            await AddMethodParameters(paramContext);

            dbContext.Curves.Count().Should().Be(2);
            dbContext.AnalyticalRuns.Count().Should().Be(1);

            // Initial curve is flagged (ID 1), will change to OK curve (ID 2)
            var initialRun = dbContext.AnalyticalRuns.First();
            initialRun.CurveId.Should().Be(1);

            var command = new ChangeCurveForAnalyticalRunCommand(1, 1);

            // Act
            await mediator.Send(command, TestContext.Current.CancellationToken);
        }

        await WaitForNotificationsAsync();

        // Assert - verify the curve was changed and accepted
        var dbContext1 = ServiceProvider.GetRequiredService<IResultDbContext>();
        var run = dbContext1.AnalyticalRuns.First();
        run.CurveId.Should().Be(1);
        run.CurveState.Should().Be(CurveState.Valid);
    }

    private static async Task AddCurveOptions(IResultDbContext dbContext, string serialNumber)
    {
        //1
        var selectedCurve = new Curve
        {
            InstrumentNumber = serialNumber,
            MethodName = _methodName,
            ConjugateLotNumber = _conjugateLot,
            Judgment = CurveJudgment.Flagged,
            MeasurementDateTime = DateTimeOffset.Now,
            LastDayToUse = DateOnly.FromDateTime(DateTimeOffset.Now.Date.AddDays(1)),
            GroupId = Guid.NewGuid()
        };

        //2
        var curveOk = new Curve
        {
            InstrumentNumber = serialNumber,
            MethodName = _methodName,
            ConjugateLotNumber = _conjugateLot,
            Judgment = CurveJudgment.OK,
            MeasurementDateTime = DateTimeOffset.Now,
            LastDayToUse = DateOnly.FromDateTime(DateTimeOffset.Now.Date.AddDays(1)),
            GroupId = Guid.NewGuid()
        };

        //3
        var curveOk2 = new Curve
        {
            InstrumentNumber = serialNumber,
            MethodName = _methodName,
            ConjugateLotNumber = _conjugateLot,
            Judgment = CurveJudgment.OK,
            MeasurementDateTime = DateTimeOffset.Now,
            LastDayToUse = DateOnly.FromDateTime(DateTimeOffset.Now.Date.AddDays(1)),
            GroupId = Guid.NewGuid()
        };

        //4
        var curveFlagged = new Curve
        {
            InstrumentNumber = serialNumber,
            MethodName = _methodName,
            ConjugateLotNumber = _conjugateLot,
            Judgment = CurveJudgment.OK,
            MeasurementDateTime = DateTimeOffset.Now,
            LastDayToUse = DateOnly.FromDateTime(DateTimeOffset.Now.Date.AddDays(1)),
            GroupId = Guid.NewGuid()
        };

        //5
        var curveNotOk = new Curve
        {
            InstrumentNumber = serialNumber,
            MethodName = _methodName,
            ConjugateLotNumber = _conjugateLot,
            Judgment = CurveJudgment.NotOK,
            MeasurementDateTime = DateTimeOffset.Now,
            LastDayToUse = DateOnly.FromDateTime(DateTimeOffset.Now.Date.AddDays(1)),
            GroupId = Guid.NewGuid()
        };

        //6
        var curveOld = new Curve
        {
            InstrumentNumber = serialNumber,
            MethodName = _methodName,
            ConjugateLotNumber = _conjugateLot,
            Judgment = CurveJudgment.OK,
            MeasurementDateTime = (DateTimeOffset.Now).AddDays(-2),
            LastDayToUse = DateOnly.FromDateTime(DateTimeOffset.Now.Date.AddDays(-1)),
            GroupId = Guid.NewGuid()
        };

        //7
        var curveDiffConj = new Curve
        {
            InstrumentNumber = serialNumber,
            MethodName = _methodName,
            ConjugateLotNumber = "diff",
            Judgment = CurveJudgment.OK,
            MeasurementDateTime = DateTimeOffset.Now,
            LastDayToUse = DateOnly.FromDateTime(DateTimeOffset.Now.Date.AddDays(1)),
            GroupId = Guid.NewGuid()
        };

        //8
        var curveDiffInstrument = new Curve
        {
            InstrumentNumber = "Seed_001",
            MethodName = _methodName,
            ConjugateLotNumber = _conjugateLot,
            Judgment = CurveJudgment.OK,
            MeasurementDateTime = DateTimeOffset.Now,
            LastDayToUse = DateOnly.FromDateTime(DateTimeOffset.Now.Date.AddDays(1)),
            GroupId = Guid.NewGuid()
        };

        //9
        var curveDiffMethod = new Curve
        {
            InstrumentNumber = serialNumber,
            MethodName = "El-G",
            ConjugateLotNumber = _conjugateLot,
            Judgment = CurveJudgment.OK,
            MeasurementDateTime = DateTimeOffset.Now,
            LastDayToUse = DateOnly.FromDateTime(DateTimeOffset.Now.Date.AddDays(1)),
            GroupId = Guid.NewGuid()
        };

        await dbContext.Curves.AddAsync(selectedCurve, TestContext.Current.CancellationToken);
        await dbContext.Curves.AddAsync(curveOk, TestContext.Current.CancellationToken);
        await dbContext.Curves.AddAsync(curveOk2, TestContext.Current.CancellationToken);
        await dbContext.Curves.AddAsync(curveFlagged, TestContext.Current.CancellationToken);
        await dbContext.Curves.AddAsync(curveNotOk, TestContext.Current.CancellationToken);
        await dbContext.Curves.AddAsync(curveOld, TestContext.Current.CancellationToken);
        await dbContext.Curves.AddAsync(curveDiffConj, TestContext.Current.CancellationToken);
        await dbContext.Curves.AddAsync(curveDiffInstrument, TestContext.Current.CancellationToken);
        await dbContext.Curves.AddAsync(curveDiffMethod, TestContext.Current.CancellationToken);

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static async Task AddMethodParameters(IParametersDbContext paramContext)
    {
        var fixture = new Fixture();
        var method = new Method
        {
            MethodName = _methodName,
            Technology = AssayTechnology.ImmunoCap,
            FullMethodName = "ImmunoCap Specific IgE 0-100",
            MethodNameLis = _methodName,
            Enabled = true,
            AnalyticalRun = new MethodAnalyticalRun(),
            Calculation = new MethodCalculation(),
            Presentation = fixture.Create<MethodPresentation>(),
            Reporting = fixture.Create<MethodReporting>(),
            Reagents =
            [
                new MethodReagent
                {
                    ReagentType = ReagentType.Conjugate,
                    DefaultReagentIdentity = _methodName
                }
            ],
            ConcentrationUnits =
            [
                new MethodConcentrationUnit
                {
                    UnitId = 1,
                    ConcentrationUnit = "kUA/l"
                }
            ]
        };

        paramContext.Methods.Add(method);

        var methodTest = new MethodTest
        {
            TestName = "c1",
            DefaultLabTestName = "c1",
            FullTestName = "c1",
            DiluentIdentity = "Dil",
            DefaultConcentrationUnit = method.ConcentrationUnits[0],
            Method = method
        };
        paramContext.MethodTests.Add(methodTest);

        var methodCalibration = new MethodCalibrationCurve
        {
            MethodId = 1,
            MethodTestId = 1,
            Method = method,
            MethodTest = methodTest,
            DefaultMaxNumberOfDays = 1
        };

        paramContext.MethodCalibrationCurves.Add(methodCalibration);

        var calibratorPoints = new MethodCalibrationPoint
        {
            CalibratorName = "CAL-1",
            MethodCalibrationCurveId = 1,
            ExpectedConcentration = 1,
            Replicates = 0,
            AdjustmentForAlfaBeta1Beta2 = 0,
            IdealResponse = 0,
            ExpectedStdDev = 0,
            UpperResponseLimit = 0,
            LowerResponseLimit = 0,
            StdDevLimit = 0,
            StdDevPrecisionLimit = 0,
            UpperLogarithmicLimit = null,
            LowerLogarithmicLimit = null,
            CalibrationCurve = methodCalibration
        };

        paramContext.MethodCalibrationPoints.Add(calibratorPoints);
        await paramContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static async Task AddSampleTest(string serialNumber, ResultApprovalStatus approvalStatus, TestResultStatus resultStatus, IResultDbContext dbContext)
    {
        var sampleTestResult = new SampleTestResult()
        {
            AnalyticalRunId = 1,
            Status = resultStatus,
            ApprovalStatus = approvalStatus,
            InstrumentNumber = serialNumber,
            TestName = "c1",
            MethodName = _methodName,
            MeanConcentrationAsText = "1.0",
            MeanConcentration = 1,
            MeanConcentrationStatus = ConcentrationStatus.Ok,
            MeanResponse = 1,
            MeanCutOff = "Positive",
            RequestUid =
            {
                SampleId = "Sample123",
                RequestCreatedDate = DateTimeOffset.Now
            },
            TestOrderId = Guid.NewGuid(),
            Replicates = new List<TestReplicate>
            {
                new SampleTestReplicate
                {
                    Concentration = 1,
                    Response = 1
                }
            }
        };

        dbContext.SampleTestResults.Add(sampleTestResult);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static async Task AddAnalyticalRun(string serialNumber, IResultDbContext dbContext)
    {
        var ar = new AnalyticalRun
        {
            MethodName = _methodName,
            InstrumentNumber = serialNumber,
            CalibrationCurveIssue = AnalyticalRunIssue.Warning,
            ConjugateLotNumber = _conjugateLot,
            CurveId = 1,
            StartDateTime = DateTimeOffset.Now
        };

        dbContext.AnalyticalRuns.Add(ar);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static async Task AddCurve(IResultDbContext dbContext, string instrumentNumber)
    {
        const string conjugateLot = "conjL";

        var curveFlagged = new Curve
        {
            InstrumentNumber = instrumentNumber,
            MethodName = _methodName,
            ConjugateLotNumber = conjugateLot,
            Judgment = CurveJudgment.Flagged,
            MeasurementDateTime = DateTimeOffset.Now,
            GroupId = Guid.NewGuid()
        };

        var curveOk = new Curve
        {
            InstrumentNumber = instrumentNumber,
            MethodName = _methodName,
            ConjugateLotNumber = conjugateLot,
            Judgment = CurveJudgment.OK,
            MeasurementDateTime = DateTimeOffset.Now,
            GroupId = Guid.NewGuid()
        };

        await dbContext.Curves.AddAsync(curveFlagged, TestContext.Current.CancellationToken);
        await dbContext.Curves.AddAsync(curveOk, TestContext.Current.CancellationToken);

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
