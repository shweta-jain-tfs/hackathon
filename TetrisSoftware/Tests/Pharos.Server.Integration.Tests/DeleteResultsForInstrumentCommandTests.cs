using System.Globalization;
using Google.Protobuf.WellKnownTypes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Application.Result;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Result.Application.Commands;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos.TestResult;

namespace Pharos.Server.Integration.Tests;

[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestsCollection))]
public class DeleteResultsForInstrumentCommandTests : IntegrationTestBase
{
    private const string _devSolutionBarcode = "C4KXXXNZ0001";
    private const string _instrumentNumber = "SN001";

    public DeleteResultsForInstrumentCommandTests(IntegrationTestsWaf factory) : base(factory)
    {
    }

    [Fact]
    public async Task ShouldDeleteTestResults()
    {
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();

        dbContext.TestResults.Count().Should().Be(0);
        dbContext.TestReplicates.Count().Should().Be(0);

        // Create result.
        var notification = new ProcessAssayResultCommand(_instrumentNumber, CreateAssayMessage(Guid.NewGuid()));
        await mediator.Send(notification, TestContext.Current.CancellationToken);

        dbContext.TestResults.Count().Should().Be(1);
        dbContext.TestReplicates.Count().Should().Be(1);

        // Delete result.
        var requestDelete = new DeleteResultsForInstrumentCommand(_instrumentNumber);
        await mediator.Send(requestDelete, TestContext.Current.CancellationToken);

        dbContext.TestResults.Count().Should().Be(0);
        dbContext.TestReplicates.Count().Should().Be(0);
    }

    [Fact]
    public async Task ShouldDeleteAnalyticalRunsAndCurves()
    {
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();

        var analyticalRun = await CreateSomeAnalyticRunsAndUseLast(dbContext);

        var methodName = analyticalRun.MethodName;
        var analyticalRunId = analyticalRun.InstrumentAnalyticalRunId.ToString();
        var runId = string.Empty;

        // End analytical run.
        await mediator.Send(new EndAnalyticalRunCommand(_instrumentNumber, methodName, analyticalRunId, runId), TestContext.Current.CancellationToken);

        dbContext.AnalyticalRuns.Count().Should().Be(3);
        dbContext.Curves.Count().Should().Be(3);

        // Delete result.
        var requestDelete = new DeleteResultsForInstrumentCommand(_instrumentNumber);
        await mediator.Send(requestDelete, TestContext.Current.CancellationToken);

        dbContext.AnalyticalRuns.Count().Should().Be(0);
        dbContext.Curves.Count().Should().Be(0);
    }

    private static AssayResult CreateAssayMessage(Guid instrumentReplicateId)
    {
        var assay = new AssayResult
        {
            TestType = EnumTestType.Sample,
            TimeOfMeasurement = Timestamp.FromDateTimeOffset(DateTimeOffset.Parse("2023-09-29 11:17:00", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal)),
            MethodName = "sIgE",
            TestName = "c1",
            InstrumentDilutionFactor = 1,
            Sample = new SampleInformation
            {
                SampleId = "B4KXXXNZ0001",
                SampleDate = Timestamp.FromDateTimeOffset(DateTimeOffset.Parse("2023-09-29 11:17:00", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal)),
                PreDilutionFactor = 1,
            },
            Replicate = new ReplicateInfo
            {
                ReplicateId = instrumentReplicateId.ToString(),
                GroupId = Guid.NewGuid().ToString(),
                ReplicatesInTestOrder = 2,
            },
            Run = new RunInfo
            {
                AnalyticalRunId = Guid.NewGuid().ToString(),
                AssayRunId = Guid.NewGuid().ToString(),
            },
            FluorometerResult = new FluorometerValue
            {
                MeasurementValue = 3000
            },
            Reagents =
            {
                new ReagentInformation
                {
                    ReagentType = EnumReagentType.ImmunoCapCarrier,
                    ReagentBarcode = "C4KXXXNZ0001",
                    ReagentUdiBarcode = "|01510222222333361715070710A213B1|991244",
                    CellPosition = 1,
                    ExpiredReagent = false
                },
                new ReagentInformation
                {
                    ReagentType = EnumReagentType.DevelopmentSolutionCartridge,
                    ReagentBarcode = _devSolutionBarcode,
                    ReagentUdiBarcode = "",
                    CellPosition = 1,
                    ExpiredReagent = false
                }
            }
        };
        return assay;
    }

    private static async Task<AnalyticalRun> CreateSomeAnalyticRunsAndUseLast(IResultDbContext db)
    {
        await CreateAnalyticalRunAndAddToDb(db, _instrumentNumber);
        await CreateAnalyticalRunAndAddToDb(db, _instrumentNumber);
        var ar = await CreateAnalyticalRunAndAddToDb(db, _instrumentNumber);
        return ar;
    }

    private static async Task<AnalyticalRun> CreateAnalyticalRunAndAddToDb(IResultDbContext db, string instrumentNumber)
    {
        var method = "asfd";

        var curve = new Curve()
        {
            InstrumentNumber = instrumentNumber,
            MethodName = method,
            ApprovalStatus = ResultApprovalStatus.NotApproved,
            CalculationType = CalculationType.Rodbard4Par,
            CurveFittedFlag = true,
            Judgment = CurveJudgment.OK,
            GroupId = Guid.NewGuid(),
            InstrumentRunId = Guid.NewGuid()
        };
        db.Curves.Add(curve);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var ar = new AnalyticalRun()
        {
            InstrumentNumber = instrumentNumber,
            StartDateTime = DateTimeOffset.Now,
            MethodName = method,
            CurveId = curve.Id,
            InstrumentAnalyticalRunId = Guid.NewGuid(),
        };
        db.AnalyticalRuns.Add(ar);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        return ar;
    }

}