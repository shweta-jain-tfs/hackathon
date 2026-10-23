using Moq.EntityFrameworkCore;
using Pharos.Server.Application.Security;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Parameters.Application.Services;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;


namespace Pharos.Server.Result.Application.Commands;

public class EvaluateCurveControlsCommandTests
{
    private const string _instrumentNumber = "SN001";

    private readonly Mock<IResultDbContext> _dbContextMock = new();
    private readonly Mock<IParametersDbContext> _parametersContextMock = new();
    private readonly EvaluateCurveControlsCommandHandler _sut;

    private int _resultId;
    private int _analyticalRunId;

    public EvaluateCurveControlsCommandTests()
    {
        var methodsService = new MethodsService(_parametersContextMock.Object, new Mock<IAuthorizationService>().Object);
        _sut = new EvaluateCurveControlsCommandHandler(_dbContextMock.Object, methodsService);
        PrepareMethodParameters();
    }

    [Theory]
    [InlineData(CurveControlJudgment.OK)]
    [InlineData(CurveControlJudgment.High)]
    [InlineData(CurveControlJudgment.Low)]
    [InlineData(CurveControlJudgment.FlaggedHigh)]
    [InlineData(CurveControlJudgment.FlaggedLow)]
    [InlineData(CurveControlJudgment.NotDefined)]
    public async Task CurveControls_CurrentOk_ImmunoCAP(CurveControlJudgment judgementFirstOldCurveControl)
    {
        var runId = Guid.NewGuid();
        var methodName = "sIgE";
        var testName = "a_IgE";
        var conjugateLotNo = "conjLot";
        var carrierLotNo = "carrierLot";

        var analyticalRuns = SetupAnalyticalRuns(runId, methodName, conjugateLotNo, carrierLotNo);

        // Old curve controls
        var groupId = Guid.NewGuid();
        var previousCurveControls = new List<CurveControlTestResult>()
        {
            CreateCurveControlResult("CC-1", judgementFirstOldCurveControl, methodName, testName, 1, groupId, judgementFirstOldCurveControl, judgementFirstOldCurveControl),
            CreateCurveControlResult("CC-2", CurveControlJudgment.OK, methodName, testName, 1, groupId, CurveControlJudgment.OK, CurveControlJudgment.OK)
        };

        // New curve controls
        groupId = Guid.NewGuid();
        var currentCurveControls = new List<CurveControlTestResult>()
        {
            CreateCurveControlResult("CC-1", CurveControlJudgment.OK, methodName, testName, 1, groupId, CurveControlJudgment.OK, CurveControlJudgment.OK),
            CreateCurveControlResult("CC-2", CurveControlJudgment.OK, methodName, testName, 1, groupId, CurveControlJudgment.OK, CurveControlJudgment.OK)
        };

        // Connections between analytical runs and replicates
        CombineAnalyticalRunAndReplicates(analyticalRuns[0], previousCurveControls);
        CombineAnalyticalRunAndReplicates(analyticalRuns[1], currentCurveControls);

        // All curve controls
        SetupCurveControlResults(previousCurveControls, currentCurveControls);

        // Run the test
        var totalJudgement = await _sut.Handle(new EvaluateCurveControlsCommand(currentCurveControls, methodName, groupId, runId, _instrumentNumber, null, conjugateLotNo, carrierLotNo), TestContext.Current.CancellationToken);

        // Check the judgment
        totalJudgement.Should().Be(CurveControlJudgmentResult.OK);
    }

    [Theory]
    [InlineData(CurveControlJudgment.OK)]
    [InlineData(CurveControlJudgment.High)]
    [InlineData(CurveControlJudgment.Low)]
    [InlineData(CurveControlJudgment.FlaggedHigh)]
    [InlineData(CurveControlJudgment.FlaggedLow)]
    [InlineData(CurveControlJudgment.NotDefined)]
    public async Task CurveControls_CurrentOk_EliA(CurveControlJudgment judgementFirstOldCurveControl)
    {
        var runId = Guid.NewGuid();
        var methodName = "El-G";
        var testName = "Gcal";
        var conjugateLotNo = "conjLot";
        var carrierLotNo = "carrierLot";

        var analyticalRuns = SetupAnalyticalRuns(runId, methodName, conjugateLotNo, carrierLotNo);

        // Old curve controls
        var groupId = Guid.NewGuid();
        var previousCurveControls = new List<CurveControlTestResult>()
        {
            CreateCurveControlResult("CC-1", judgementFirstOldCurveControl, methodName, testName, 2, groupId, judgementFirstOldCurveControl, judgementFirstOldCurveControl),
        };

        // New curve controls
        groupId = Guid.NewGuid();
        var currentCurveControls = new List<CurveControlTestResult>()
        {
            CreateCurveControlResult("CC-1", CurveControlJudgment.OK, methodName, testName, 2, groupId, CurveControlJudgment.OK, CurveControlJudgment.OK),
        };

        // Connections between analytical runs and replicates
        CombineAnalyticalRunAndReplicates(analyticalRuns[0], previousCurveControls);
        CombineAnalyticalRunAndReplicates(analyticalRuns[1], currentCurveControls);

        // All curve controls
        SetupCurveControlResults(previousCurveControls, currentCurveControls);

        // Run the test
        var totalJudgement = await _sut.Handle(new EvaluateCurveControlsCommand(currentCurveControls, methodName, groupId, runId, _instrumentNumber, null, conjugateLotNo, carrierLotNo), TestContext.Current.CancellationToken);

        // Check the judgment
        totalJudgement.Should().Be(CurveControlJudgmentResult.OK);
    }

    [Theory]
    [InlineData(CurveControlJudgment.OK, CurveControlJudgment.OK, (int)CurveControlJudgmentResult.OK)]
    [InlineData(CurveControlJudgment.High, CurveControlJudgment.OK, (int)CurveControlJudgmentResult.NotOK)]
    [InlineData(CurveControlJudgment.OK, CurveControlJudgment.Low, (int)CurveControlJudgmentResult.NotOK)]
    [InlineData(CurveControlJudgment.FlaggedLow, CurveControlJudgment.FlaggedHigh, (int)CurveControlJudgmentResult.NotOK)]
    [Trait("ModuleRequirement", "MR_3519")]
    public async Task TotalJudgementOfCurveControl_NotOK_ImmunoCAP(
        CurveControlJudgment judgementFirstReplicate, CurveControlJudgment judgementSecondReplicate, int expectedTotalJudgement)
    {
        var runId = Guid.NewGuid();
        var methodName = "sIgE";
        var testName = "a_IgE";
        var conjugateLotNo = "conjLot";
        var carrierLotNo = "carrierLot";

        var analyticalRuns = SetupAnalyticalRuns(runId, methodName, conjugateLotNo, carrierLotNo);

        // Old curve controls
        var groupId = Guid.NewGuid();
        var previousCurveControls = new List<CurveControlTestResult>()
        {
            CreateCurveControlResult("CC-1", CurveControlJudgment.OK, methodName, testName, 1, groupId, CurveControlJudgment.OK, CurveControlJudgment.OK),
            CreateCurveControlResult("CC-2", CurveControlJudgment.OK, methodName, testName, 1, groupId, CurveControlJudgment.OK, CurveControlJudgment.OK)
        };

        // New curve controls
        groupId = Guid.NewGuid();
        var currentCurveControls = new List<CurveControlTestResult>()
        {
            CreateCurveControlResult("CC-1", CurveControlJudgment.OK, methodName, testName, 1, groupId, judgementFirstReplicate, CurveControlJudgment.OK),
            CreateCurveControlResult("CC-2", CurveControlJudgment.OK, methodName, testName, 1, groupId, judgementSecondReplicate, CurveControlJudgment.OK)
        };

        // Connections between analytical runs and replicates
        CombineAnalyticalRunAndReplicates(analyticalRuns[0], previousCurveControls);
        CombineAnalyticalRunAndReplicates(analyticalRuns[1], currentCurveControls);

        // All curve controls
        SetupCurveControlResults(previousCurveControls, currentCurveControls);

        // Run the test
        var totalJudgement = await _sut.Handle(new EvaluateCurveControlsCommand(currentCurveControls, methodName, groupId, runId, _instrumentNumber, null, conjugateLotNo, carrierLotNo), TestContext.Current.CancellationToken);

        // Check the judgment
        totalJudgement.Should().Be((CurveControlJudgmentResult)expectedTotalJudgement);
    }

    [Theory]
    [InlineData(CurveControlJudgment.OK, CurveControlJudgment.OK, (int)CurveControlJudgmentResult.OK)]
    [InlineData(CurveControlJudgment.High, CurveControlJudgment.OK, (int)CurveControlJudgmentResult.NotOK)]
    [InlineData(CurveControlJudgment.OK, CurveControlJudgment.Low, (int)CurveControlJudgmentResult.NotOK)]
    [InlineData(CurveControlJudgment.FlaggedLow, CurveControlJudgment.FlaggedHigh, (int)CurveControlJudgmentResult.NotOK)]
    [Trait("ModuleRequirement", "MR_3519")]
    public async Task TotalJudgementOfCurveControl_NotOK_EliA(
        CurveControlJudgment judgementFirstReplicate, CurveControlJudgment judgementSecondReplicate, int expectedTotalJudgement)
    {
        var runId = Guid.NewGuid();
        var methodName = "El-G";
        var testName = "Gcal";
        var conjugateLotNo = "conjLot";
        var carrierLotNo = "carrierLot";

        var analyticalRuns = SetupAnalyticalRuns(runId, methodName, conjugateLotNo, carrierLotNo);

        // Old curve controls
        var groupId = Guid.NewGuid();
        var previousCurveControls = new List<CurveControlTestResult>()
        {
            CreateCurveControlResult("CC-1", CurveControlJudgment.OK, methodName, testName, 2, groupId, CurveControlJudgment.OK, CurveControlJudgment.OK),
        };

        // New curve controls
        groupId = Guid.NewGuid();
        var currentCurveControls = new List<CurveControlTestResult>()
        {
            CreateCurveControlResult("CC-1", CurveControlJudgment.OK, methodName, testName, 2, groupId, judgementFirstReplicate, judgementSecondReplicate),
        };

        // Connections between analytical runs and replicates
        CombineAnalyticalRunAndReplicates(analyticalRuns[0], previousCurveControls);
        CombineAnalyticalRunAndReplicates(analyticalRuns[1], currentCurveControls);

        // All curve controls
        SetupCurveControlResults(previousCurveControls, currentCurveControls);

        // Run the test
        var totalJudgement = await _sut.Handle(new EvaluateCurveControlsCommand(currentCurveControls, methodName, groupId, runId, _instrumentNumber, null, conjugateLotNo, carrierLotNo), TestContext.Current.CancellationToken);

        // Check the judgment
        totalJudgement.Should().Be((CurveControlJudgmentResult)expectedTotalJudgement);
    }

    [Theory]
    [InlineData(CurveControlJudgment.OK, CurveControlJudgment.OK, (int)CurveControlJudgmentResult.OK, false, false)]
    [InlineData(CurveControlJudgment.FlaggedHigh, CurveControlJudgment.OK, (int)CurveControlJudgmentResult.Flagged, false, false)]
    [InlineData(CurveControlJudgment.OK, CurveControlJudgment.FlaggedLow, (int)CurveControlJudgmentResult.Flagged, false, false)]
    [InlineData(CurveControlJudgment.OK, CurveControlJudgment.OK, (int)CurveControlJudgmentResult.OK, true, false)]
    [InlineData(CurveControlJudgment.OK, CurveControlJudgment.OK, (int)CurveControlJudgmentResult.OK, false, true)]
    [InlineData(CurveControlJudgment.OK, CurveControlJudgment.OK, (int)CurveControlJudgmentResult.Flagged, true, true)]
    [Trait("ModuleRequirement", "MR_3520")]
    public async Task TotalJudgementOfCurveControl_Flagged_ImmunoCAP(
        CurveControlJudgment judgementFirstReplicate, CurveControlJudgment judgementSecondReplicate, int expectedTotalJudgement, bool firstReplicateAborted, bool secondReplicateAborted)
    {
        var runId = Guid.NewGuid();
        var methodName = "sIgE";
        var testName = "a_IgE";
        var conjugateLotNo = "conjLot";
        var carrierLotNo = "carrierLot";

        var analyticalRuns = SetupAnalyticalRuns(runId, methodName, conjugateLotNo, carrierLotNo);

        // Old curve controls
        var groupId = Guid.NewGuid();
        var previousCurveControls = new List<CurveControlTestResult>()
        {
            CreateCurveControlResult("CC-1", CurveControlJudgment.OK, methodName, testName, 1, groupId, CurveControlJudgment.OK, CurveControlJudgment.OK),
            CreateCurveControlResult("CC-2", CurveControlJudgment.OK, methodName, testName, 1, groupId, CurveControlJudgment.OK, CurveControlJudgment.OK)
        };

        // New curve controls
        groupId = Guid.NewGuid();
        var currentCurveControls = new List<CurveControlTestResult>()
        {
            CreateCurveControlResult("CC-1", CurveControlJudgment.OK, methodName, testName, 1, groupId, judgementFirstReplicate, CurveControlJudgment.OK, firstReplicateAborted),
            CreateCurveControlResult("CC-2", CurveControlJudgment.OK, methodName, testName, 1, groupId, judgementSecondReplicate, CurveControlJudgment.OK, secondReplicateAborted)
        };

        // Connections between analytical runs and replicates
        CombineAnalyticalRunAndReplicates(analyticalRuns[0], previousCurveControls);
        CombineAnalyticalRunAndReplicates(analyticalRuns[1], currentCurveControls);

        // All curve controls
        SetupCurveControlResults(previousCurveControls, currentCurveControls);

        // Run the test
        var totalJudgement = await _sut.Handle(new EvaluateCurveControlsCommand(currentCurveControls, methodName, groupId, runId, _instrumentNumber, null, conjugateLotNo, carrierLotNo), TestContext.Current.CancellationToken);

        // Check the judgment
        totalJudgement.Should().Be((CurveControlJudgmentResult)expectedTotalJudgement);
    }

    [Theory]
    [InlineData(CurveControlJudgment.OK, CurveControlJudgment.OK, (int)CurveControlJudgmentResult.OK, false, false)]
    [InlineData(CurveControlJudgment.FlaggedHigh, CurveControlJudgment.OK, (int)CurveControlJudgmentResult.Flagged, false, false)]
    [InlineData(CurveControlJudgment.OK, CurveControlJudgment.FlaggedLow, (int)CurveControlJudgmentResult.Flagged, false, false)]
    [InlineData(CurveControlJudgment.OK, CurveControlJudgment.OK, (int)CurveControlJudgmentResult.OK, true, false)]
    [InlineData(CurveControlJudgment.OK, CurveControlJudgment.OK, (int)CurveControlJudgmentResult.OK, false, true)]
    [InlineData(CurveControlJudgment.OK, CurveControlJudgment.OK, (int)CurveControlJudgmentResult.Flagged, true, true)]
    [Trait("ModuleRequirement", "MR_3520")]
    public async Task TotalJudgementOfCurveControl_Flagged_EliA(
        CurveControlJudgment judgementFirstReplicate, CurveControlJudgment judgementSecondReplicate, int expectedTotalJudgement, bool firstReplicateAborted, bool secondReplicateAborted)
    {
        var runId = Guid.NewGuid();
        var methodName = "El-G";
        var testName = "Gcal";
        var conjugateLotNo = "conjLot";
        var carrierLotNo = "carrierLot";

        var analyticalRuns = SetupAnalyticalRuns(runId, methodName, conjugateLotNo, carrierLotNo);

        // Old curve controls
        var groupId = Guid.NewGuid();
        var previousCurveControls = new List<CurveControlTestResult>()
        {
            CreateCurveControlResult("CC-1", CurveControlJudgment.OK, methodName, testName, 2, groupId, CurveControlJudgment.OK, CurveControlJudgment.OK),
        };

        // New curve controls
        groupId = Guid.NewGuid();
        var currentCurveControls = new List<CurveControlTestResult>()
        {
            CreateCurveControlResult("CC-1", CurveControlJudgment.OK, methodName, testName, 2, groupId, judgementFirstReplicate, judgementSecondReplicate, firstReplicateAborted, secondReplicateAborted),
        };

        // Connections between analytical runs and replicates
        CombineAnalyticalRunAndReplicates(analyticalRuns[0], previousCurveControls);
        CombineAnalyticalRunAndReplicates(analyticalRuns[1], currentCurveControls);

        // All curve controls
        SetupCurveControlResults(previousCurveControls, currentCurveControls);

        // Run the test
        var totalJudgement = await _sut.Handle(new EvaluateCurveControlsCommand(currentCurveControls, methodName, groupId, runId, _instrumentNumber, null, conjugateLotNo, carrierLotNo), TestContext.Current.CancellationToken);

        // Check the judgment
        totalJudgement.Should().Be((CurveControlJudgmentResult)expectedTotalJudgement);
    }

    [Theory]
    [InlineData(CurveControlJudgment.OK, CurveControlJudgment.OK, (int)CurveControlJudgmentResult.OK, false, false)]
    [InlineData(CurveControlJudgment.FlaggedHigh, CurveControlJudgment.OK, (int)CurveControlJudgmentResult.ReRunNotOK, false, false)]
    [InlineData(CurveControlJudgment.OK, CurveControlJudgment.FlaggedLow, (int)CurveControlJudgmentResult.ReRunNotOK, false, false)]
    [InlineData(CurveControlJudgment.OK, CurveControlJudgment.OK, (int)CurveControlJudgmentResult.OK, true, false)]
    [InlineData(CurveControlJudgment.OK, CurveControlJudgment.OK, (int)CurveControlJudgmentResult.OK, false, true)]
    [InlineData(CurveControlJudgment.OK, CurveControlJudgment.OK, (int)CurveControlJudgmentResult.ReRunNotOK, true, true)]
    [Trait("ModuleRequirement", "MR_3518")]
    public async Task TotalJudgementOfCurveControl_ReRunNotOk_ImmunoCAP(
        CurveControlJudgment judgementFirstReplicate, CurveControlJudgment judgementSecondReplicate, int expectedTotalJudgement, bool firstReplicateAborted, bool secondReplicateAborted)
    {
        var runId = Guid.NewGuid();
        var methodName = "sIgE";
        var testName = "a_IgE";
        var conjugateLotNo = "conjLot";
        var carrierLotNo = "carrierLot";

        var analyticalRuns = SetupAnalyticalRuns(runId, methodName, conjugateLotNo, carrierLotNo);

        // Old curve controls
        var groupId = Guid.NewGuid();
        var previousCurveControls = new List<CurveControlTestResult>()
        {
            CreateCurveControlResult("CC-1", CurveControlJudgment.FlaggedLow, methodName, testName, 1, groupId, CurveControlJudgment.OK, CurveControlJudgment.OK),
            CreateCurveControlResult("CC-2", CurveControlJudgment.OK, methodName, testName, 1, groupId, CurveControlJudgment.OK, CurveControlJudgment.OK)
        };

        // New curve controls
        groupId = Guid.NewGuid();
        var currentCurveControls = new List<CurveControlTestResult>()
        {
            CreateCurveControlResult("CC-1", CurveControlJudgment.OK, methodName, testName, 1, groupId, judgementFirstReplicate, CurveControlJudgment.OK, firstReplicateAborted),
            CreateCurveControlResult("CC-2", CurveControlJudgment.OK, methodName, testName, 1, groupId, judgementSecondReplicate, CurveControlJudgment.OK, secondReplicateAborted)
        };

        // Connections between analytical runs and replicates
        CombineAnalyticalRunAndReplicates(analyticalRuns[0], previousCurveControls);
        CombineAnalyticalRunAndReplicates(analyticalRuns[1], currentCurveControls);

        // All curve controls
        SetupCurveControlResults(previousCurveControls, currentCurveControls);

        // Run the test
        var totalJudgement = await _sut.Handle(new EvaluateCurveControlsCommand(currentCurveControls, methodName, groupId, runId, _instrumentNumber, null, conjugateLotNo, carrierLotNo), TestContext.Current.CancellationToken);

        // Check the judgment
        totalJudgement.Should().Be((CurveControlJudgmentResult)expectedTotalJudgement);
    }

    [Theory]
    [InlineData(CurveControlJudgment.OK, CurveControlJudgment.OK, (int)CurveControlJudgmentResult.OK, false, false)]
    [InlineData(CurveControlJudgment.FlaggedHigh, CurveControlJudgment.OK, (int)CurveControlJudgmentResult.ReRunNotOK, false, false)]
    [InlineData(CurveControlJudgment.OK, CurveControlJudgment.FlaggedLow, (int)CurveControlJudgmentResult.ReRunNotOK, false, false)]
    [InlineData(CurveControlJudgment.OK, CurveControlJudgment.OK, (int)CurveControlJudgmentResult.OK, true, false)]
    [InlineData(CurveControlJudgment.OK, CurveControlJudgment.OK, (int)CurveControlJudgmentResult.OK, false, true)]
    [InlineData(CurveControlJudgment.OK, CurveControlJudgment.OK, (int)CurveControlJudgmentResult.ReRunNotOK, true, true)]
    [Trait("ModuleRequirement", "MR_3518")]
    public async Task TotalJudgementOfCurveControl_ReRunNotOk_EliA(
        CurveControlJudgment judgementFirstReplicate, CurveControlJudgment judgementSecondReplicate, int expectedTotalJudgement, bool firstReplicateAborted, bool secondReplicateAborted)
    {
        var runId = Guid.NewGuid();
        var methodName = "El-G";
        var testName = "Gcal";
        var conjugateLotNo = "conjLot";
        var carrierLotNo = "carrierLot";

        var analyticalRuns = SetupAnalyticalRuns(runId, methodName, conjugateLotNo, carrierLotNo);

        // Old curve controls
        var groupId = Guid.NewGuid();
        var previousCurveControls = new List<CurveControlTestResult>()
        {
            CreateCurveControlResult("CC-1", CurveControlJudgment.FlaggedLow, methodName, testName, 2, groupId, CurveControlJudgment.OK, CurveControlJudgment.OK),
        };

        // New curve controls
        groupId = Guid.NewGuid();
        var currentCurveControls = new List<CurveControlTestResult>()
        {
            CreateCurveControlResult("CC-1", CurveControlJudgment.OK, methodName, testName, 2, groupId, judgementFirstReplicate, judgementSecondReplicate, firstReplicateAborted, secondReplicateAborted),
        };

        // Connections between analytical runs and replicates
        CombineAnalyticalRunAndReplicates(analyticalRuns[0], previousCurveControls);
        CombineAnalyticalRunAndReplicates(analyticalRuns[1], currentCurveControls);

        // All curve controls
        SetupCurveControlResults(previousCurveControls, currentCurveControls);

        // Run the test
        var totalJudgement = await _sut.Handle(new EvaluateCurveControlsCommand(currentCurveControls, methodName, groupId, runId, _instrumentNumber, null, conjugateLotNo, carrierLotNo), TestContext.Current.CancellationToken);

        // Check the judgment
        totalJudgement.Should().Be((CurveControlJudgmentResult)expectedTotalJudgement);
    }

    private List<AnalyticalRun> SetupAnalyticalRuns(Guid runId, string methodName, string conjugateLotNo, string carrierLotNo)
    {
        var analyticalRuns = new List<AnalyticalRun>()
        {
            CreateAnalyticalRun(runId, methodName, conjugateLotNo, carrierLotNo),
            CreateAnalyticalRun(runId, methodName, conjugateLotNo, carrierLotNo),
        };
        _dbContextMock.Setup(db => db.AnalyticalRuns).ReturnsDbSet(analyticalRuns);

        return analyticalRuns;
    }

    private AnalyticalRun CreateAnalyticalRun(Guid runId, string methodName, string conjugateLotNo, string carrierLotNo)
    {
        var ar = new AnalyticalRun()
        {
            Id = _analyticalRunId++,
            InstrumentNumber = _instrumentNumber,
            InstrumentRunId = runId,
            InstrumentAnalyticalRunId = Guid.NewGuid(),
            MethodName = methodName,
            ConjugateLotNumber = conjugateLotNo,
            CarrierLotNumber = carrierLotNo,
        };

        return ar;
    }

    private CurveControlTestResult CreateCurveControlResult(string ccName, CurveControlJudgment judgmentSummary, string methodName, string testName, int replicates, Guid groupId,
        CurveControlJudgment replicateJudgement1, CurveControlJudgment replicateJudgement2, bool replicateAborted1 = false, bool replicateAborted2 = false)
    {
        var cc = new CurveControlTestResult()
        {
            Id = _resultId++,
            CurveControlPointName = ccName,
            InstrumentNumber = _instrumentNumber,
            JudgmentSummary = judgmentSummary,
            MethodName = methodName,
            TestName = testName,
            NumberOfReplicates = replicates,
            GroupId = groupId,
            MeanConcentration = 10,
            Replicates = new List<TestReplicate>()
        };

        var replicateJudgement = replicateJudgement1;
        var replicateAborted = replicateAborted1;
        for (var nRepl = 0; nRepl < replicates; nRepl++)
        {
            cc.Replicates.Add(CreateCurveControlReplicate(cc, replicateJudgement, replicateAborted));
            replicateJudgement = replicateJudgement2;
            replicateAborted = replicateAborted2;
        }

        return cc;
    }

    private static CurveControlTestReplicate CreateCurveControlReplicate(CurveControlTestResult cc, CurveControlJudgment judgement, bool aborted)
    {
        var replicate = new CurveControlTestReplicate()
        {
            TestResultId = cc.Id,
            TestResult = cc,
            Judgment = judgement,
            Concentration = 10,
            InstrumentReplicateId = Guid.NewGuid(),
            IsAborted = aborted
        };

        return replicate;
    }

    private static void CombineAnalyticalRunAndReplicates(AnalyticalRun analyticalRun, List<CurveControlTestResult> curveControls)
    {
        foreach (var ccResult in curveControls)
        {
            ccResult.AnalyticalRun = analyticalRun;
        }
    }

    private void SetupCurveControlResults(List<CurveControlTestResult> previousCurveControls, List<CurveControlTestResult> currentCurveControls)
    {
        var allCurveControls = new List<CurveControlTestResult>(previousCurveControls);
        allCurveControls.AddRange(currentCurveControls);
        _dbContextMock.Setup(db => db.CurveControlTestResults).ReturnsDbSet(allCurveControls);

        SetupCurveControlTestReplicates(allCurveControls);
    }

    private void SetupCurveControlTestReplicates(List<CurveControlTestResult> curveControls)
    {
        var replicates = new List<CurveControlTestReplicate>();

        foreach (var ccResult in curveControls)
        {
            replicates.AddRange(ccResult.Replicates!.Cast<CurveControlTestReplicate>());
        }

        _dbContextMock.Setup(db => db.TestReplicates).ReturnsDbSet(replicates);
    }

    /// <summary>
    /// Prepare method parameters
    /// </summary>
    private void PrepareMethodParameters()
    {
        var methodTests = new List<MethodTest>
        {
            new()
            {
                TestName = "c1",
                ConcentrationFactor = 1,
                UseLotSpecificCode = false,
                IterativeAlfaBeta1Beta2Flag = false,
                AdjustmentForAlfaBeta1Beta2 = 1,
                HighestConcentrationToReport = null,
                LowerDetectionLimit = 0,
                ReportConcentrationBelowCurveFlag = false,
                ReportConcentrationAboveCurveFlag = false,
                DefaultCutOffConcentration = 0.35,
                DefaultReportCutOff = true,
                DefaultReportConcentration = true,
                DefaultReportClassId = 2,
                DefaultReportQuotient = QuotientReportMode.ReportAsFraction,
                DefaultReplicates = 1,

                Method = new Method
                {
                    MethodName = "sIgE",
                    AnalyticalRun = new(),
                    Calculation = new MethodCalculation(),
                    Reporting = new MethodReporting(),
                    CalibrationCurve = new MethodCalibrationCurve(),
                    CurveControls = new List<MethodCurveControl>
                    {
                        new()
                        {
                            CurveControlName = "CC-1",
                            ExpectedConcentration = 10.0,
                            InnerStdDevLimit = 1,
                            ExpectedStdDev = 5,
                            OuterStdDevLimit = 2,
                        },
                        new()
                        {
                            CurveControlName = "CC-2",
                            ExpectedConcentration = 50.0,
                            InnerStdDevLimit = 1,
                            ExpectedStdDev = 5,
                            OuterStdDevLimit = 2,
                        }
                    },
                    ClassTableBelows = new List<MethodClassTableBelow>(),
                    ClassTables = new List<MethodClassTable>(),
                },
            },
            new()
            {
                TestName = "Gcal",
                ConcentrationFactor = 10,
                UseLotSpecificCode = false,
                IterativeAlfaBeta1Beta2Flag = false,
                AdjustmentForAlfaBeta1Beta2 = 1,
                HighestConcentrationToReport = null,
                LowerDetectionLimit = 0,
                ReportConcentrationBelowCurveFlag = false,
                ReportConcentrationAboveCurveFlag = false,
                DefaultCutOffConcentration = null,
                DefaultReportCutOff = false,
                DefaultReportConcentration = true,
                DefaultReportClassId = 0,
                DefaultReportQuotient = QuotientReportMode.ReportAsFraction,
                DefaultReplicates = 1,

                Method = new Method
                {
                    MethodName = "El-G",
                    AnalyticalRun = new(),
                    Calculation = new MethodCalculation(),
                    Reporting = new MethodReporting(),
                    CalibrationCurve = new MethodCalibrationCurve(),
                    CurveControls = new List<MethodCurveControl>
                    {
                        new()
                        {
                            CurveControlName = "CC-1",
                            ExpectedConcentration = 10.0,
                            InnerStdDevLimit = 1,
                            ExpectedStdDev = 5,
                            OuterStdDevLimit = 2,
                        },
                    },
                    ClassTableBelows = new List<MethodClassTableBelow>(),
                    ClassTables = new List<MethodClassTable>(),
                },
            }
        };
        _parametersContextMock.Setup(db => db.MethodTests).ReturnsDbSet(methodTests);

        var methods = methodTests.Select(x => x.Method).ToList();
        _parametersContextMock.Setup(db => db.Methods).ReturnsDbSet(methods);
    }
}
