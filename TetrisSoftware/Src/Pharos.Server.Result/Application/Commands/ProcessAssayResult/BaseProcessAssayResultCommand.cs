using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Application.SystemErrors;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos.TestResult;

namespace Pharos.Server.Result.Application.Commands.ProcessAssayResult;

internal abstract class BaseProcessAssayResultCommand : IRequest
{
    public string InstrumentNumber { get; set; } = null!;
    public AssayResult Message { get; set; } = null!;
    public bool AllowRepeatedMessage { get; set; }
}

internal abstract class ProcessAssayResultCommandBaseHandler<TCommand, TTestResult, TTestReplicate> : IRequestHandler<TCommand>
    where TCommand : BaseProcessAssayResultCommand
    where TTestResult : TestResult, new()
    where TTestReplicate : TestReplicate, new()
{
    protected AnalyticalRun? AnalyticalRun;
    protected readonly IReagentBarcodeService BarcodeService;
    protected string ConjugateLotNumber = string.Empty;
    protected string CarrierLotNumber = string.Empty;
    protected TTestResult? CurrentTestResult;
    protected TTestReplicate? CurrentReplicate;
    protected readonly IResultDbContext DbContext;
    protected readonly IMethodsService MethodsService;
    protected readonly IMediator Mediator;
    protected Guid InstrumentReplicateId;
    protected readonly ILogger<ProcessAssayResultCommandBaseHandler<TCommand, TTestResult, TTestReplicate>> Logger;

    private string _carrierBarcode = string.Empty;
    private readonly IResultFormatter _resultFormatter;
    private readonly IResponseCalculationService _responseCalculationService;
    private readonly IResultCalculationService _resultCalculationService;

    protected ProcessAssayResultCommandBaseHandler(
        IResultDbContext dbContext,
        IMethodsService methodsService,
        ILogger<ProcessAssayResultCommandBaseHandler<TCommand, TTestResult, TTestReplicate>> logger,
        IResultCalculationService resultCalculationService,
        IResponseCalculationService responseCalculationService,
        IResultFormatter resultFormatter,
        IMediator mediator,
        IReagentBarcodeService barcodeService)
    {
        DbContext = dbContext;
        MethodsService = methodsService;
        Logger = logger;
        _resultCalculationService = resultCalculationService;
        _responseCalculationService = responseCalculationService;
        _resultFormatter = resultFormatter;
        Mediator = mediator;
        BarcodeService = barcodeService;
    }

    protected virtual bool ReplicateConcentrationNeeded => true;

    protected abstract Task<TTestResult?> GetTestResultWithExistingReplicates(string instrumentNumber, AssayResult message, CancellationToken cancellationToken);

    public async Task Handle(TCommand request, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Started processing an assay result: Instrument = {Instrument}, TestType = {TestType}", request.InstrumentNumber, request.Message.TestType);

        var instrumentNumber = request.InstrumentNumber;
        var message = request.Message;

        var reagents = request.Message.Reagents;

        // extract conjugate lot number
        var conjugateBarcode = reagents.Where(b => b.ReagentType == EnumReagentType.ConjugateCartridge).Select(b => b.ReagentBarcode).FirstOrDefault();
        if (conjugateBarcode != null)
        {
            ConjugateLotNumber = BarcodeService.GetReagentLotInformation(ArticleType.ConjugateCartridge, conjugateBarcode)?.LotNumber ?? "";
        }

        // extract carrier lot number
        var carrier = reagents.Where(b => b.ReagentType is EnumReagentType.ImmunoCapCarrier or EnumReagentType.EliaWellCarrier or EnumReagentType.EliaCalibrationWellCarrier)
            .Select(b => new { b.ReagentType, b.ReagentBarcode }).FirstOrDefault();
        if (carrier != null)
        {
            _carrierBarcode = carrier.ReagentBarcode;
            CarrierLotNumber = BarcodeService.GetReagentLotInformation(GetArticleTypeFromReagentType(carrier.ReagentType), carrier.ReagentBarcode)?.LotNumber ?? "";
        }

        InstrumentReplicateId = Guid.Parse(message.Replicate.ReplicateId);

        if (request.AllowRepeatedMessage)
        {
            // check for existing assay result: repeated FLR-import case
            var alreadyExists = await DbContext.TestReplicates
                .Where(x => x.InstrumentReplicateId == InstrumentReplicateId)
                .AnyAsync(cancellationToken);

            if (alreadyExists)
            {
                Logger.LogInformation("Assay result ReplicateId={ReplicateId} already exists, skipping...", InstrumentReplicateId);
                return;
            }
        }

        // get analytical run
        AnalyticalRun = await GetAnalyticalRun(request.InstrumentNumber, message, cancellationToken);
        if (AnalyticalRun == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(AnalyticalRun.CmrRunId) && !string.IsNullOrEmpty(message.Run.CustomRunId))
        {
            AnalyticalRun.CmrRunId = message.Run.CustomRunId;
        }

        // Get an existing test result for this replicate (with other replicates processed earlier)
        // or create a test result.
        CurrentTestResult = await GetTestResultWithExistingReplicates(instrumentNumber, message, cancellationToken) ?? CreateTestResult(instrumentNumber, message);

        // Create and initialize a new replicate
        CurrentReplicate = CreateReplicate(message);

        CurrentTestResult.Replicates!.Add(CurrentReplicate);

        if (CurrentTestResult.AllReplicatesAvailable)
        {
            CurrentTestResult.Status = TestResultStatus.PendingBlank;
        }

        await CalculateReplicateResponse(instrumentNumber, message, CurrentReplicate);

        if (CurrentReplicate.Response != null && ReplicateConcentrationNeeded && AnalyticalRun.CanCalculateConcentrations)
        {
            await CalculateReplicateConcentration(
                CurrentReplicate,
                message.Sample.PreDilutionFactor,
                message.InstrumentDilutionFactor,
                message.MethodName,
                message.TestName,
                CurrentTestResult.ResultType,
            cancellationToken);
        }

        if (CurrentTestResult is { AllReplicatesAvailable: true, AllReplicatesHaveResponse: true })
        {
            OnAllReplicatesHaveResponse(CurrentTestResult);
        }

        await DbContext.SaveChangesAsync(cancellationToken);
    }

    protected virtual async Task CalculateReplicateResponse(string instrumentNumber, AssayResult requestMessage, TTestReplicate replicate)
    {
        if (replicate.IsAborted)
        {
            return;
        }

        var devSolution = requestMessage.Reagents.FirstOrDefault(b => b.ReagentType == EnumReagentType.DevelopmentSolutionCartridge);
        if (devSolution == null)
        {
            Logger.LogError("The assay result does not contain the development solution barcode: {InstrumentReplicateId}", replicate.InstrumentReplicateId);
            return;
        }
        var blankType = await ReagentBlankTypeUsedForMethod(requestMessage.MethodName);
        var blank = await _responseCalculationService.GetValidReagentBlank(instrumentNumber, devSolution.ReagentBarcode, devSolution.CellPosition, blankType);

        if (blank == null)
        {
            return;
        }

        await _responseCalculationService.CalculateResponseForTestReplicate(replicate, blank, CurrentTestResult!.ResultType, requestMessage.MethodName, requestMessage.Sample.SampleId);
    }


    protected virtual TTestResult CreateTestResult(string instrumentNumber, AssayResult message)
    {
        var testResult = new TTestResult
        {
            AnalyticalRunId = AnalyticalRun!.Id,
            InstrumentNumber = instrumentNumber,
            MethodName = message.MethodName,
            TestName = message.TestName,
            InstrumentDilutionFactor = message.InstrumentDilutionFactor,
            PreDilutionFactor = message.Sample.PreDilutionFactor,
            NumberOfReplicates = message.Replicate.ReplicatesInTestOrder,
            MeasurementDateTime = message.TimeOfMeasurement.ToDateTimeOffset().ToLocalTime(),
            Replicates = new List<TestReplicate>(),
            Status = TestResultStatus.Processing
        };

        DbContext.TestResults.Add(testResult);

        return testResult;
    }

    protected virtual void OnAllReplicatesHaveResponse(TTestResult testResult)
    {
        _responseCalculationService.CalculateMeanResponse(testResult);
        testResult.Status = TestResultStatus.PendingCalibration;
    }


    /// <summary>
    /// Calculate concentration for one replicate
    /// </summary>
    private async Task CalculateReplicateConcentration(TestReplicate replicate, int preDilutionFactor, int instrumentDilutionFactor, string methodName, string testName, TestResultType testOrderType, CancellationToken cancellationToken)
    {
        var curveId = AnalyticalRun!.CurveId;

        if (curveId != null)
        {
            await _resultCalculationService.CalculateReplicateConcentration(
                replicate,
                curveId.Value,
                preDilutionFactor,
                instrumentDilutionFactor,
                methodName,
                testName,
                testOrderType,
                cancellationToken);
        }
        else
        {
            replicate.ConcentrationAsText = _resultFormatter.NoDataAvailable();
        }
    }

    private TTestReplicate CreateReplicate(AssayResult message)
    {
        var replicate = new TTestReplicate
        {
            InstrumentReplicateId = Guid.Parse(message.Replicate.ReplicateId),
            ReplicateNumber = message.Replicate.ReplicateNumber,
            MeasurementDateTime = message.TimeOfMeasurement.ToDateTimeOffset().ToLocalTime(),
            CmrTestId = message.Replicate.CmrTestId,
            IrwRow = message.IrwRow,
            IrwSpoke = message.IrwSpoke,
            ErwRow = message.ErwRow,
            ErwSpoke = message.ErwSpoke,
            IsAborted = message.IsAborted,
            FluorometerValue = message.IsAborted ? null : message.FluorometerResult.MeasurementValue,
            CarrierBarcode = _carrierBarcode,
        };
        replicate.TestReplicateErrors ??= new List<TestReplicateError>();
        replicate.TestReplicateReagents ??= new List<TestReplicateReagent>();

        // add replicate reagents
        foreach (var reagent in message.Reagents)
        {
            var articleType = GetArticleTypeFromReagentType(reagent.ReagentType);
            replicate.TestReplicateReagents ??= new List<TestReplicateReagent>();

            var reagentLotInformation = BarcodeService.GetReagentLotInformation(articleType, reagent.ReagentBarcode, reagent.ReagentUdiBarcode);
            var serialNumber = BarcodeService.GetSerialNumber(articleType, reagent.ReagentBarcode, reagent.ReagentUdiBarcode);

            var rAdd = new TestReplicateReagent
            {
                ReagentType = articleType,
                Barcode = reagent.ReagentBarcode ?? "",
                Udi = reagent.ReagentUdiBarcode ?? "",
                LotNumber = reagentLotInformation?.LotNumber ?? "",
                SerialNumber = serialNumber ?? "",
                CellPosition = reagent.CellPosition,
                Expired = reagent.ExpiredReagent
            };

            // Multiple dilution steps can use the same reagent cell. Store each reagent key once per replicate.
            if (!replicate.TestReplicateReagents.Any(r => r.Barcode == rAdd.Barcode
                && r.SerialNumber == rAdd.SerialNumber
                && r.CellPosition == rAdd.CellPosition
                && r.LotNumber == rAdd.LotNumber
                && r.Expired == rAdd.Expired
                && r.ReagentType == rAdd.ReagentType))
            {
                replicate.TestReplicateReagents.Add(rAdd);
            }
        }

        // add replicate errors
        foreach (var error in message.Errors)
        {
            var e = new TestReplicateError
            {
                ErrorCode = (int)error
            };

            replicate.TestReplicateErrors ??= new List<TestReplicateError>();
            replicate.TestReplicateErrors.Add(e);
        }

        InitializeCurrentReplicate(replicate, message);

        DbContext.TestReplicates.Add(replicate);
        return replicate;
    }

    protected virtual void InitializeCurrentReplicate(TTestReplicate replicate, AssayResult message)
    {
    }

    private async Task<BlankType> ReagentBlankTypeUsedForMethod(string methodName)
    {
        var methodTechnology = await MethodsService.GetMethod(methodName, m => m.Technology);

        if (methodTechnology == AssayTechnology.EliA)
        {
            return BlankType.ReagentBlankEliA;
        }

        return BlankType.ReagentBlankImmunoCap;
    }

    /// <summary>
    /// Retrieve the analytical run where current assay result information is included in
    /// If not found, the analytical run is created
    /// </summary>
    private async Task<AnalyticalRun?> GetAnalyticalRun(string instrumentNumber, AssayResult message, CancellationToken cancellationToken)
    {
        var runInfo = message.Run;

        // Theoretically, an assay result might come without run info
        // We'll assign empty GUIDs as a "catch all" scenario, to avoid skipping results
        var analyticalRunIdGuid = string.IsNullOrWhiteSpace(runInfo.AnalyticalRunId) ? Guid.Empty : Guid.Parse(runInfo.AnalyticalRunId);
        var runId = string.IsNullOrWhiteSpace(runInfo.AssayRunId) ? Guid.Empty : Guid.Parse(runInfo.AssayRunId);

        var analyticalRun = await DbContext.AnalyticalRuns.FirstOrDefaultAsync(a => a.InstrumentNumber == instrumentNumber && a.InstrumentAnalyticalRunId == analyticalRunIdGuid, cancellationToken);

        if (analyticalRun == null)
        {
            analyticalRun = await Mediator.Send(new StartAnalyticalRunCommand
            {
                AnalyticalRunId = analyticalRunIdGuid,
                InstrumentNumber = instrumentNumber,
                StartDateTime = message.TimeOfMeasurement.ToDateTimeOffset().ToLocalTime(),
                MethodName = message.MethodName,
                ConjugateLotNumber = ConjugateLotNumber,
                CarrierLotNumber = CarrierLotNumber,
                CustomRunId = message.Run.CustomRunId,
                RunId = runId
            }, cancellationToken);
        }
        else if (analyticalRun.EndDateTime != null)
        {
            await Mediator.Send(new CreatePharosErrorCommand(
               PharosErrorCodes.ResultReceivedAfterRunEnd,
               ErrorClassification.Warning,
               instrumentNumber,
               [message.MethodName, message.TestName, analyticalRunIdGuid.ToString(), message.Replicate.ReplicateId]), cancellationToken);

            Logger.LogWarning("Discarded assay result received after analytical run end: Instrument = {Instrument}, MethodName = {MethodName}, TestName = {TestName}, AnalyticalRunId = {AnalyticalRunId}, ReplicateId = {ReplicateId}",
                instrumentNumber, message.MethodName, message.TestName, analyticalRunIdGuid, message.Replicate.ReplicateId);

            return null;
        }
        return analyticalRun;
    }

    private static ArticleType GetArticleTypeFromReagentType(EnumReagentType reagentType)
    {
        return reagentType switch
        {
            EnumReagentType.ImmunoCapCarrier => ArticleType.ImmunoCapCarrier,
            EnumReagentType.EliaWellCarrier => ArticleType.EliaWellCarrier,
            EnumReagentType.EliaCalibrationWellCarrier => ArticleType.EliaCalibratorWellCarrier,
            EnumReagentType.ConjugateCartridge => ArticleType.ConjugateCartridge,
            EnumReagentType.DevelopmentSolutionCartridge => ArticleType.DevelopmentSolutionCartridge,
            EnumReagentType.StopSolution => ArticleType.StopSolutionBottle,
            EnumReagentType.CalibratorCartridge => ArticleType.CalibratorCartridge,
            EnumReagentType.CurveControlCartridge => ArticleType.CurveControlCartridge,
            EnumReagentType.QualityControlCartridge => ArticleType.QualityControlCartridge,
            EnumReagentType.DiluentCartridge => ArticleType.DiluentCartridge,
            EnumReagentType.DilutionWellCarrier => ArticleType.DilutionWellCarrier,
            EnumReagentType.WashAdditive => ArticleType.WashAdditiveBottle,
            EnumReagentType.WashConcentrate => ArticleType.WashConcentrateBottle,
            EnumReagentType.MaintenanceConcentrate => ArticleType.MaintenanceConcentrateBottle,
            EnumReagentType.FluoroC => ArticleType.FluoroCVial,
            EnumReagentType.QualityControlTube => throw new NotImplementedException("Quality control tubes are not yet supported."),
            EnumReagentType.UnspecifiedReagentType => throw new ValidationException("Unspecified reagent type."),
            EnumReagentType.DilutionPlate => throw new ArgumentOutOfRangeException(nameof(reagentType), reagentType, null),
            _ => throw new ArgumentOutOfRangeException(nameof(reagentType), reagentType, null)
        };
    }
}
