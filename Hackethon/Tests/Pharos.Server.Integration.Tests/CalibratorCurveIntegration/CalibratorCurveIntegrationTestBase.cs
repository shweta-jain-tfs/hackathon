using Google.Protobuf.WellKnownTypes;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Result;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Result.Application.Commands;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos.TestResult;
using Method = Pharos.Server.Domain.Parameters.Methods.Method;

namespace Pharos.Server.Integration.Tests.CalibratorCurveIntegration;

/// <summary>
/// Domain-specific base class for calibrator curve and curve control integration tests.
/// Inherits full WAF/DI/database-respawn infrastructure from <see cref="IntegrationTestBase"/>.
///
/// Two interaction patterns are supported:
///   1. Protocol layer  – send messages via the mediator exactly as the instrument would
///                        (<see cref="SendCalibratorReplicateAsync"/>, <see cref="SendCurveControlReplicateAsync"/>).
///   2. Direct seeding  – use <see cref="CreateTestData"/> to build and persist domain entities
///                        for tests that need pre-existing state (curves, analytical runs, etc.).
/// </summary>
public abstract class CalibratorCurveIntegrationTestBase(IntegrationTestsWaf factory) : IntegrationTestBase(factory)
{
    protected const string DefaultMethodName = "sIgE";
    protected const string DefaultConjugateLot = "BGR01";
    protected const string DefaultCalibratorCartridgeBarcode = "A5SXXXSS0001";
    protected const string DefaultCurveControlCartridgeBarcode = "A0VXXXSS0001";
    protected const string DefaultDevSolutionBarcode = "EK4XXXNZ0001";

    internal CalibrationTestSession CreateSession()
        => CalibrationTestSession.Create(Factory.Services);

    protected sealed record CalibratorPoint(
        string Name,
        double FirstMeasurementValue,
        double SecondMeasurementValue,
        bool FirstAborted = false,
        bool SecondAborted = false,
        EnumErrorCode? FirstErrorCode = null,
        EnumErrorCode? SecondErrorCode = null);

    protected sealed record CurveControlPoint(
        string Name,
        double FirstMeasurementValue,
        double? SecondMeasurementValue = null,
        bool FirstAborted = false,
        bool SecondAborted = false,
        EnumErrorCode? FirstErrorCode = null,
        EnumErrorCode? SecondErrorCode = null);

    protected sealed record SamplePoint(
        string SampleId,
        double FirstMeasurementValue,
        double? SecondMeasurementValue = null,
        bool FirstAborted = false,
        bool SecondAborted = false,
        EnumErrorCode? FirstErrorCode = null,
        EnumErrorCode? SecondErrorCode = null,
        int InstrumentDilutionFactor = 1,
        int PreDilutionFactor = 1);

    protected static double ToMeasurementValue(double response) => (response * .04) + 0.138;

    /// <summary>
    /// Returns a <see cref="CurveTestDataBuilder"/> pre-configured for <paramref name="instrumentNumber"/>
    /// and <paramref name="methodName"/>. Call <c>BuildAsync</c> on the result to persist the entities.
    /// </summary>
    internal static CurveTestDataBuilder CreateTestData(
        string instrumentNumber,
        string methodName = DefaultMethodName,
        string conjugateLot = DefaultConjugateLot)
        => new CurveTestDataBuilder()
            .ForInstrument(instrumentNumber)
            .ForMethod(methodName, conjugateLot);

    /// <summary>
    /// Sends a reagent blank result via the mediator.
    /// Must be called before sending calibrator or curve control replicates so that RU can be calculated.
    /// For calibrator curve tests, use meanValue=0 to match test data expectations.
    /// </summary>
    protected static Task SendBlankAsync(
        IMediator mediator,
        string instrumentNumber,
        string devSolutionBarcode = DefaultDevSolutionBarcode,
        double meanValue = 0)
    {
        var blank = new BlankResult
        {
            BlankType = EnumBlankType.ReagentImmunoCap,
            ResultOk = true,
            MeanValue = meanValue,
            UpperLimit = 9999,
            LowerLimit = 0,
            DevelopmentSolutionIdentity = new DevelopmentSolutionIdentity
            {
                ReagentBarcode = devSolutionBarcode,
                CellPosition = 1
            },
            StopSolutionBarcode = "DoesNotMatter"
        };

        return mediator.Send(new ProcessBlankResultCommand(instrumentNumber, blank));
    }

    /// <summary>
    /// Sends analytical-run-end via mediator.
    /// </summary>
    protected static Task SendAnalyticalRunEndAsync(
        IMediator mediator,
        string instrumentNumber,
        string methodName,
        string analyticalRunId,
        string assayRunId)
    {
        return mediator.Send(new EndAnalyticalRunCommand(instrumentNumber, methodName, analyticalRunId, assayRunId));
    }

    /// <summary>
    /// Sends rerun curve control command via mediator.
    /// </summary>
    protected static Task SendRerunCurveControlAsync(IMediator mediator, int analyticalRunId)
    {
        return mediator.Send(new RerunCurveControlCommand { AnalyticalRunId = analyticalRunId });
    }

    /// <summary>
    /// Sends rerun calibrator curve command via mediator.
    /// </summary>
    protected static Task SendRerunCalibratorCurveAsync(IMediator mediator, int analyticalRunId)
    {
        return mediator.Send(new RerunCalibratorCurveCommand(analyticalRunId));
    }

    /// <summary>
    /// Sends assay-end via mediator. By default, <paramref name="customRunId"/> is empty to avoid
    /// triggering export jobs in integration tests.
    /// </summary>
    protected static Task SendAssayEndAsync(
        IMediator mediator,
        string instrumentNumber,
        string assayRunId,
        string customRunId = "")
    {
        return mediator.Send(new EndAssayRunCommand(instrumentNumber, assayRunId, customRunId));
    }

    /// <summary>
    /// Sends a single calibrator replicate via the mediator, simulating a result arriving from the
    /// instrument over the Tetris protocol. All replicates that belong to the same calibration run
    /// must share the same <paramref name="calibratorGroupId"/>.
    /// </summary>
    protected static Task SendCalibratorReplicateAsync(
        IMediator mediator,
        string instrumentNumber,
        string analyticalRunId,
        Guid calibratorGroupId,
        string calibratorPointName,
        double fluorometerValue = 15000,
        string methodName = DefaultMethodName,
        string cartridgeBarcode = DefaultCalibratorCartridgeBarcode)
    {
        var message = BuildAssayMessage(
            replicateId: Guid.NewGuid(),
            groupId: calibratorGroupId,
            analyticalRunId: analyticalRunId,
            assayRunId: Guid.NewGuid().ToString(),
            testType: EnumTestType.Calibrator,
            reagentType: EnumReagentType.CalibratorCartridge,
            methodName: methodName,
            sampleId: calibratorPointName,
            testName: calibratorPointName,
            cartridgeBarcode: cartridgeBarcode,
            fluorometerValue: fluorometerValue);

        return mediator.Send(new ProcessAssayResultCommand(instrumentNumber, message));
    }

    /// <summary>
    /// Sends a complete calibrator group using simulator-like payload semantics:
    /// one test order per calibrator point, two replicates per test order, shared group id,
    /// stable analytical/assay run ids and method-specific default carrier/conjugate barcodes.
    /// </summary>
    protected static async Task<Guid> SendCalibratorGroupAsync(
        IMediator mediator,
        string instrumentNumber,
        string analyticalRunId,
        string assayRunId,
        string testName,
        IReadOnlyList<CalibratorPoint> calibrators,
        string methodName = DefaultMethodName,
        string calibratorBarcode = DefaultCalibratorCartridgeBarcode,
        string? carrierBarcode = null,
        string? conjugateBarcode = null,
        CancellationToken cancellationToken = default)
    {
        if (calibrators.Count == 0)
        {
            throw new ArgumentException("At least one calibrator point must be provided.", nameof(calibrators));
        }

        var groupId = Guid.NewGuid();
        var resolvedCarrierBarcode = carrierBarcode ?? GetDefaultCarrierBarcode(methodName);
        var resolvedConjugateBarcode = conjugateBarcode ?? GetDefaultConjugateBarcode(methodName);

        for (var index = 0; index < calibrators.Count; index++)
        {
            var calibrator = calibrators[index];
            var testOrderNumber = index + 1;

            var firstReplicate = BuildAssayMessage(
                replicateId: Guid.NewGuid(),
                groupId: groupId,
                analyticalRunId: analyticalRunId,
                assayRunId: assayRunId,
                testType: EnumTestType.Calibrator,
                reagentType: EnumReagentType.CalibratorCartridge,
                methodName: methodName,
                sampleId: calibrator.Name,
                testName: testName,
                cartridgeBarcode: calibratorBarcode,
                fluorometerValue: calibrator.FirstMeasurementValue,
                testOrdersInGroup: calibrators.Count,
                testOrderNumber: testOrderNumber,
                replicatesInTestOrder: 2,
                replicateNumber: 1,
                isAborted: calibrator.FirstAborted,
                carrierBarcode: resolvedCarrierBarcode,
                conjugateBarcode: resolvedConjugateBarcode);

            if (calibrator.FirstErrorCode.HasValue)
            {
                firstReplicate.Errors.Add(calibrator.FirstErrorCode.Value);
            }

            await mediator.Send(new ProcessAssayResultCommand(instrumentNumber, firstReplicate), cancellationToken);

            var secondReplicate = BuildAssayMessage(
                replicateId: Guid.NewGuid(),
                groupId: groupId,
                analyticalRunId: analyticalRunId,
                assayRunId: assayRunId,
                testType: EnumTestType.Calibrator,
                reagentType: EnumReagentType.CalibratorCartridge,
                methodName: methodName,
                sampleId: calibrator.Name,
                testName: testName,
                cartridgeBarcode: calibratorBarcode,
                fluorometerValue: calibrator.SecondMeasurementValue,
                testOrdersInGroup: calibrators.Count,
                testOrderNumber: testOrderNumber,
                replicatesInTestOrder: 2,
                replicateNumber: 2,
                isAborted: calibrator.SecondAborted,
                carrierBarcode: resolvedCarrierBarcode,
                conjugateBarcode: resolvedConjugateBarcode);

            if (calibrator.SecondErrorCode.HasValue)
            {
                secondReplicate.Errors.Add(calibrator.SecondErrorCode.Value);
            }

            await mediator.Send(new ProcessAssayResultCommand(instrumentNumber, secondReplicate), cancellationToken);
        }

        return groupId;
    }

    /// <summary>
    /// Sends a complete curve control group using simulator-like payload semantics:
    /// one test order per curve control point, 1 or 2 replicates per test order, shared group id,
    /// stable analytical/assay run ids and method-specific default carrier/conjugate/curve-control barcodes.
    /// For method "View", two loops are used by default to mirror simulator behavior.
    /// </summary>
    protected static async Task<Guid> SendCurveControlGroupAsync(
        IMediator mediator,
        string instrumentNumber,
        string analyticalRunId,
        string assayRunId,
        string testName,
        IReadOnlyList<CurveControlPoint> curveControls,
        string methodName = DefaultMethodName,
        string? curveControlBarcode = null,
        string? carrierBarcode = null,
        string? conjugateBarcode = null,
        int? loops = null,
        CancellationToken cancellationToken = default)
    {
        if (curveControls.Count == 0)
        {
            throw new ArgumentException("At least one curve control point must be provided.", nameof(curveControls));
        }

        var groupId = Guid.NewGuid();
        var resolvedCarrierBarcode = carrierBarcode ?? GetDefaultCarrierBarcode(methodName);
        var resolvedConjugateBarcode = conjugateBarcode ?? GetDefaultConjugateBarcode(methodName);
        var resolvedCurveControlBarcode = curveControlBarcode ?? GetDefaultCurveControlBarcode(methodName);
        var nLoops = loops ?? (methodName == "View" ? 2 : 1);

        for (var pointIndex = 0; pointIndex < curveControls.Count; pointIndex++)
        {
            var curveControl = curveControls[pointIndex];
            var testOrderNumber = pointIndex + 1;
            var hasSecondReplicate = curveControl.SecondMeasurementValue.HasValue && curveControl.SecondMeasurementValue.Value > 0;
            var replicateCountPerLoop = hasSecondReplicate ? 2 : 1;
            var replicatesInTestOrder = replicateCountPerLoop * nLoops;
            var replicateNumber = 1;

            for (var loopIndex = 0; loopIndex < nLoops; loopIndex++)
            {
                var firstReplicate = BuildAssayMessage(
                    replicateId: Guid.NewGuid(),
                    groupId: groupId,
                    analyticalRunId: analyticalRunId,
                    assayRunId: assayRunId,
                    testType: EnumTestType.CurveControl,
                    reagentType: EnumReagentType.CurveControlCartridge,
                    methodName: methodName,
                    sampleId: curveControl.Name,
                    testName: testName,
                    cartridgeBarcode: resolvedCurveControlBarcode,
                    fluorometerValue: curveControl.FirstMeasurementValue,
                    testOrdersInGroup: curveControls.Count,
                    testOrderNumber: testOrderNumber,
                    replicatesInTestOrder: replicatesInTestOrder,
                    replicateNumber: replicateNumber++,
                    isAborted: curveControl.FirstAborted,
                    carrierBarcode: resolvedCarrierBarcode,
                    conjugateBarcode: resolvedConjugateBarcode);

                if (curveControl.FirstErrorCode.HasValue)
                {
                    firstReplicate.Errors.Add(curveControl.FirstErrorCode.Value);
                }

                await mediator.Send(new ProcessAssayResultCommand(instrumentNumber, firstReplicate), cancellationToken);

                if (!hasSecondReplicate)
                {
                    continue;
                }

                var secondReplicate = BuildAssayMessage(
                    replicateId: Guid.NewGuid(),
                    groupId: groupId,
                    analyticalRunId: analyticalRunId,
                    assayRunId: assayRunId,
                    testType: EnumTestType.CurveControl,
                    reagentType: EnumReagentType.CurveControlCartridge,
                    methodName: methodName,
                    sampleId: curveControl.Name,
                    testName: testName,
                    cartridgeBarcode: resolvedCurveControlBarcode,
                    fluorometerValue: curveControl.SecondMeasurementValue!.Value,
                    testOrdersInGroup: curveControls.Count,
                    testOrderNumber: testOrderNumber,
                    replicatesInTestOrder: replicatesInTestOrder,
                    replicateNumber: replicateNumber++,
                    isAborted: curveControl.SecondAborted,
                    carrierBarcode: resolvedCarrierBarcode,
                    conjugateBarcode: resolvedConjugateBarcode);

                if (curveControl.SecondErrorCode.HasValue)
                {
                    secondReplicate.Errors.Add(curveControl.SecondErrorCode.Value);
                }

                await mediator.Send(new ProcessAssayResultCommand(instrumentNumber, secondReplicate), cancellationToken);
            }
        }

        return groupId;
    }

    /// <summary>
    /// Sends a single curve control replicate via the mediator, simulating a result arriving from the
    /// instrument over the Tetris protocol.
    /// </summary>
    protected static Task SendCurveControlReplicateAsync(
        IMediator mediator,
        string instrumentNumber,
        string analyticalRunId,
        string curveControlPointName,
        double fluorometerValue = 15000,
        string methodName = DefaultMethodName,
        string cartridgeBarcode = DefaultCurveControlCartridgeBarcode,
        string? testName = null)
    {
        var message = BuildAssayMessage(
            replicateId: Guid.NewGuid(),
            groupId: Guid.NewGuid(),
            analyticalRunId: analyticalRunId,
            assayRunId: Guid.NewGuid().ToString(),
            testType: EnumTestType.CurveControl,
            reagentType: EnumReagentType.CurveControlCartridge,
            methodName: methodName,
            sampleId: curveControlPointName,
            testName: testName ?? curveControlPointName,
            cartridgeBarcode: cartridgeBarcode,
            fluorometerValue: fluorometerValue);

        return mediator.Send(new ProcessAssayResultCommand(instrumentNumber, message));
    }

    /// <summary>
    /// Sends sample results using simulator-like payload semantics:
    /// one test order per sample point, 1 or 2 replicates per test order and stable analytical/assay run ids.
    /// </summary>
    protected static async Task SendSampleGroupAsync(
        IMediator mediator,
        string instrumentNumber,
        string analyticalRunId,
        string assayRunId,
        string methodName,
        string testName,
        IReadOnlyList<SamplePoint> samples,
        string? carrierBarcode = null,
        string? conjugateBarcode = null,
        CancellationToken cancellationToken = default)
    {
        if (samples.Count == 0)
        {
            throw new ArgumentException("At least one sample point must be provided.", nameof(samples));
        }

        var resolvedCarrierBarcode = carrierBarcode ?? GetDefaultSampleCarrierBarcode(methodName);
        var resolvedConjugateBarcode = conjugateBarcode ?? GetDefaultConjugateBarcode(methodName);

        foreach (var sample in samples)
        {
            var hasSecondReplicate = sample.SecondMeasurementValue.HasValue && sample.SecondMeasurementValue.Value > 0;
            var replicatesInTestOrder = hasSecondReplicate ? 2 : 1;
            var groupId = Guid.NewGuid();

            var firstReplicate = BuildAssayMessage(
                replicateId: Guid.NewGuid(),
                groupId: groupId,
                analyticalRunId: analyticalRunId,
                assayRunId: assayRunId,
                testType: EnumTestType.Sample,
                reagentType: EnumReagentType.UnspecifiedReagentType,
                methodName: methodName,
                sampleId: sample.SampleId,
                testName: testName,
                cartridgeBarcode: string.Empty,
                fluorometerValue: sample.FirstMeasurementValue,
                testOrdersInGroup: 1,
                testOrderNumber: 1,
                replicatesInTestOrder: replicatesInTestOrder,
                replicateNumber: 1,
                isAborted: sample.FirstAborted,
                carrierBarcode: resolvedCarrierBarcode,
                conjugateBarcode: resolvedConjugateBarcode,
                instrumentDilutionFactor: sample.InstrumentDilutionFactor,
                preDilutionFactor: sample.PreDilutionFactor);

            if (sample.FirstErrorCode.HasValue)
            {
                firstReplicate.Errors.Add(sample.FirstErrorCode.Value);
            }

            await mediator.Send(new ProcessAssayResultCommand(instrumentNumber, firstReplicate), cancellationToken);

            if (!hasSecondReplicate)
            {
                continue;
            }

            var secondReplicate = BuildAssayMessage(
                replicateId: Guid.NewGuid(),
                groupId: groupId,
                analyticalRunId: analyticalRunId,
                assayRunId: assayRunId,
                testType: EnumTestType.Sample,
                reagentType: EnumReagentType.UnspecifiedReagentType,
                methodName: methodName,
                sampleId: sample.SampleId,
                testName: testName,
                cartridgeBarcode: string.Empty,
                fluorometerValue: sample.SecondMeasurementValue!.Value,
                testOrdersInGroup: 1,
                testOrderNumber: 1,
                replicatesInTestOrder: replicatesInTestOrder,
                replicateNumber: 2,
                isAborted: sample.SecondAborted,
                carrierBarcode: resolvedCarrierBarcode,
                conjugateBarcode: resolvedConjugateBarcode,
                instrumentDilutionFactor: sample.InstrumentDilutionFactor,
                preDilutionFactor: sample.PreDilutionFactor);

            if (sample.SecondErrorCode.HasValue)
            {
                secondReplicate.Errors.Add(sample.SecondErrorCode.Value);
            }

            await mediator.Send(new ProcessAssayResultCommand(instrumentNumber, secondReplicate), cancellationToken);
        }
    }

    /// <summary>
    /// Sets the instrument state to <see cref="InstrumentState.Assay"/> so that commands
    /// that require an active assay (e.g. <c>RerunCurveControlCommand</c>) are not silently skipped.
    /// </summary>
    protected static void SetInstrumentInAssayMode(IServiceScope scope, string serialNumber)
    {
        var instrumentManager = scope.ServiceProvider.GetRequiredService<IInstrumentManager>();
        var instrument = (Instrument.Domain.Instrument)instrumentManager.GetInstrument(serialNumber);
        instrument.State = InstrumentState.Assay;
    }

    private static AssayResult BuildAssayMessage(
        Guid replicateId,
        Guid groupId,
        string analyticalRunId,
        string assayRunId,
        EnumTestType testType,
        EnumReagentType reagentType,
        string methodName,
        string sampleId,
        string testName,
        string cartridgeBarcode,
        double fluorometerValue,
        int testOrdersInGroup = 1,
        int testOrderNumber = 1,
        int replicatesInTestOrder = 1,
        int replicateNumber = 1,
        bool isAborted = false,
        string carrierBarcode = "409XX01",
        string conjugateBarcode = "BGRXXX010001",
        int instrumentDilutionFactor = 1,
        int preDilutionFactor = 1)
    {
        var message = new AssayResult
        {
            TestType = testType,
            MethodName = methodName,
            TestName = testName,
            TimeOfMeasurement = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow),
            InstrumentDilutionFactor = instrumentDilutionFactor,
            IsAborted = isAborted,
            Sample = new SampleInformation
            {
                SampleId = sampleId,
                SampleDate = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow),
                PreDilutionFactor = preDilutionFactor
            },
            Replicate = new ReplicateInfo
            {
                ReplicateId = replicateId.ToString(),
                GroupId = groupId.ToString(),
                ReplicatesInTestOrder = replicatesInTestOrder,
                ReplicateNumber = replicateNumber,
                TestOrdersInGroup = testOrdersInGroup,
                TestOrderNumber = testOrderNumber
            },
            Run = new RunInfo
            {
                AnalyticalRunId = analyticalRunId,
                AssayRunId = assayRunId
            },
            FluorometerResult = new FluorometerValue
            {
                MeasurementValue = fluorometerValue
            },
            Reagents =
            {
                new ReagentInformation { ReagentType = EnumReagentType.ImmunoCapCarrier, ReagentBarcode = carrierBarcode, CellPosition = 1 },
                new ReagentInformation { ReagentType = EnumReagentType.DevelopmentSolutionCartridge, ReagentBarcode = DefaultDevSolutionBarcode, CellPosition = 1 },
                new ReagentInformation { ReagentType = EnumReagentType.ConjugateCartridge, ReagentBarcode = conjugateBarcode, CellPosition = 1 },
                new ReagentInformation { ReagentType = EnumReagentType.StopSolution, ReagentBarcode = "ES1XXX010001", CellPosition = 1 }
            }
        };

        // Only add the cartridge reagent if the type is specified (not for samples)
        if (reagentType != EnumReagentType.UnspecifiedReagentType)
        {
            message.Reagents.Add(new ReagentInformation { ReagentType = reagentType, ReagentBarcode = cartridgeBarcode, CellPosition = 1 });
        }

        return message;
    }

    private static string GetDefaultConjugateBarcode(string methodName)
    {
        return methodName switch
        {
            "sIgE" => "BGRXXX010001",
            "tIgE" => "BJ5XXX010001",
            "El-G" => "BVUXXX010001",
            "El-T" => "DFWXXX010001",
            "View" => "DHUXXX010001",
            "ECP" => "A3YXXX010001",
            "Tryp" => "D05XXX010001",
            "sIgG" => "A3TXXX010001",
            "IgG4" => "C8UXXX010001",
            "El-A" => "BRDXXX010001",
            "ElC2" => "D2YXXX010001",
            "ElGM" => "CX7XXX010001",
            "El-M" => "CMGXXX010001",
            _ => throw new ArgumentOutOfRangeException(nameof(methodName), methodName, "Unknown method name.")
        };
    }

    /// <summary>
    /// Seeds the sIgE method, its calibration curve definition, calibration points, and CC definition
    /// into the Parameters database. Idempotent — skips if the method already exists.
    /// </summary>
    internal static async Task SeedMethodForCurveAndCcAsync(
        IParametersDbContext paramContext,
        string sampleTestName = "c1",
        string ccTestName = "a-IgE",
        string ccPointName = "CC-1",
        string calibratorTestName = "a_IgE")
    {
        var existingMethod = await paramContext.Methods
            .FirstOrDefaultAsync(m => m.MethodName == DefaultMethodName, TestContext.Current.CancellationToken);

        if (existingMethod != null)
        {
            var existingCurveControls = await paramContext.MethodCurveControls
                .Where(cc => cc.Method.MethodName == DefaultMethodName)
                .ToListAsync(TestContext.Current.CancellationToken);

            var existingCalibrationPoints = await paramContext.MethodCalibrationPoints
                .Where(cp => cp.CalibrationCurve.Method.MethodName == DefaultMethodName)
                .ToListAsync(TestContext.Current.CancellationToken);

            var existingCalibrationCurves = await paramContext.MethodCalibrationCurves
                .Where(mc => mc.Method.MethodName == DefaultMethodName)
                .ToListAsync(TestContext.Current.CancellationToken);

            var existingMethodTests = await paramContext.MethodTests
                .Where(mt => mt.Method.MethodName == DefaultMethodName)
                .ToListAsync(TestContext.Current.CancellationToken);

            paramContext.MethodCurveControls.RemoveRange(existingCurveControls);
            paramContext.MethodCalibrationPoints.RemoveRange(existingCalibrationPoints);
            paramContext.MethodCalibrationCurves.RemoveRange(existingCalibrationCurves);
            paramContext.MethodTests.RemoveRange(existingMethodTests);
            paramContext.Methods.Remove(existingMethod);

            await paramContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var method = new Method
        {
            MethodName = DefaultMethodName,
            Technology = AssayTechnology.ImmunoCap,
            FullMethodName = DefaultMethodName,
            MethodNameLis = DefaultMethodName,
            Enabled = true,
            AnalyticalRun = new MethodAnalyticalRun
            {
                DefaultAllowMonthlyCalibration = true
            },
            Calculation = new MethodCalculation
            {
                CalculationType = CalculationType.Rodbard5Par,
                CalibratorStartingPoint = 0,
                CalibratorsForLeastSquareCalc = 4,
                MinCoefficientOfCorrelation = 0,
                NormalWeight = NormalWeight.Predicted,
                RobustWeight = 0,
                PredictedVarianceA = 1.72783,
                PredictedVarianceB = 0.002021
            },
            Presentation = new MethodPresentation
            {
                XAxisText = "Concentration",
                XAxisUnit = "kUA/l",
                YAxisText = "Response",
                YAxisUnit = "RU"
            },
            Reporting = new MethodReporting
            {
                DefaultTextQuantitativeAbove = ">100",
                DefaultTextQuantitativeBelow = "<0.10"
            },
            ConcentrationUnits = [new MethodConcentrationUnit { UnitId = 1, ConcentrationUnit = "kUA/l" }]
        };

        var sampleMethodTest = new MethodTest
        {
            TestName = sampleTestName,
            DefaultLabTestName = sampleTestName,
            FullTestName = sampleTestName,
            DiluentIdentity = "Dil",
            DefaultConcentrationUnit = method.ConcentrationUnits[0],
            Method = method
        };

        var ccMethodTest = new MethodTest
        {
            TestName = ccTestName,
            DefaultLabTestName = ccTestName,
            FullTestName = ccTestName,
            DiluentIdentity = "Dil",
            DefaultConcentrationUnit = method.ConcentrationUnits[0],
            Method = method
        };

        var calibratorMethodTest = new MethodTest
        {
            TestName = calibratorTestName,
            DefaultLabTestName = calibratorTestName,
            FullTestName = calibratorTestName,
            DiluentIdentity = "Dil",
            DefaultConcentrationUnit = method.ConcentrationUnits[0],
            Method = method
        };

        paramContext.Methods.Add(method);
        paramContext.MethodTests.AddRange(sampleMethodTest, ccMethodTest, calibratorMethodTest);

        var methodCalibration = new MethodCalibrationCurve
        {
            Method = method,
            MethodTest = calibratorMethodTest,
            DefaultMaxNumberOfDays = 1,
            ExcludeLowestCalibratorFromMeanDiff = true,
            MaxBadReplicates = 3
        };

        var calibrationPointSettings = new Dictionary<string, (double IdealResponse, double ExpectedStdDev, double LowerResponseLimit, double UpperResponseLimit, double LowerLogarithmicLimit, double UpperLogarithmicLimit)>
        {
            ["CAL-0.00"] = (16, 7, 0, 20021, -0.2, 0.2),
            ["CAL-0.35"] = (172, 7, 95, 20021, -0.2, 0.2),
            ["CAL-0.70"] = (320, 8, 95, 20021, -0.2, 0.2),
            ["CAL-3.50"] = (1480, 34, 95, 20021, -0.2, 0.2),
            ["CAL-17.5"] = (6352, 149, 95, 20021, -0.2, 0.2),
            ["CAL-100"] = (18619, 374, 95, 27929, -0.2, 0.2),
        };

        foreach (var point in BuildSigeCalibrationPointDefinitions())
        {
            var settings = calibrationPointSettings[point.Name];

            paramContext.MethodCalibrationPoints.Add(new MethodCalibrationPoint
            {
                CalibratorName = point.Name,
                ExpectedConcentration = point.ExpectedConcentration,
                Replicates = 2,
                AdjustmentForAlfaBeta1Beta2 = 0,
                IdealResponse = settings.IdealResponse,
                ExpectedStdDev = settings.ExpectedStdDev,
                UpperResponseLimit = settings.UpperResponseLimit,
                LowerResponseLimit = settings.LowerResponseLimit,
                StdDevLimit = 4.2,
                StdDevPrecisionLimit = 3,
                UpperLogarithmicLimit = settings.UpperLogarithmicLimit,
                LowerLogarithmicLimit = settings.LowerLogarithmicLimit,
                CalibrationCurve = methodCalibration
            });
        }

        paramContext.MethodCurveControls.Add(new MethodCurveControl
        {
            CurveControlName = ccPointName,
            Active = true,
            MethodTest = ccMethodTest,
            Method = method,
            Replicates = 1,
            ExpectedConcentration = 0.70,
            ExpectedStdDev = 0.084,
            InnerStdDevLimit = 2.2,
            OuterStdDevLimit = 3.5
        });

        await paramContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    internal static IReadOnlyList<(string Name, double ExpectedConcentration)> BuildSigeCalibrationPointDefinitions() =>
    [
        ("CAL-0.00", 0.001),
        ("CAL-0.35", 0.35),
        ("CAL-0.70", 0.7),
        ("CAL-3.50", 3.5),
        ("CAL-17.5", 17.5),
        ("CAL-100", 100)
    ];

    protected static IReadOnlyList<CalibratorPoint> BuildSigeCalibratorPoints() =>
    [
        new("CAL-0.00", ToMeasurementValue(16), ToMeasurementValue(16)),
        new("CAL-0.35", ToMeasurementValue(172), ToMeasurementValue(172)),
        new("CAL-0.70", ToMeasurementValue(320), ToMeasurementValue(320)),
        new("CAL-3.50", ToMeasurementValue(1480), ToMeasurementValue(1480)),
        new("CAL-17.5", ToMeasurementValue(6352), ToMeasurementValue(6352)),
        new("CAL-100", ToMeasurementValue(18619), ToMeasurementValue(18619)),
    ];

    private static string GetDefaultCurveControlBarcode(string methodName)
    {
        return methodName switch
        {
            "sIgE" => "A0VXXXSS0001",
            "tIgE" => "A5VXXXSS0001",
            "El-G" => "BV9XXXSS0001",
            "El-T" => "D2KXXXSS0001",
            "View" => "DHVXXXSS0001",
            "ECP" => "A6JXXXSS0001",
            "Tryp" => "A6HXXXSS0001",
            "sIgG" => "A6DXXXSS0001",
            "IgG4" => "C8WXXXSS0001",
            "El-A" => "BVMXXXSS0001",
            "ElC2" => "D2XXXSS0001",
            "ElGM" => "CX5XXXSS0001",
            "El-M" => "CB6XXXSS0001",
            _ => throw new ArgumentOutOfRangeException(nameof(methodName), methodName, "Unknown method name.")
        };
    }

    private static string GetDefaultCarrierBarcode(string methodName)
    {
        return methodName switch
        {
            "sIgE" => "818XX01",
            "tIgE" => "863XX01",
            "El-G" => "BFNEJESE2B6345",
            "El-T" => "D14EJESE2B6345",
            "View" => "DHSXX01",
            "ECP" => "866XX01",
            "Tryp" => "A1CXX01",
            "sIgG" => "E4EXX01",
            "IgG4" => "EERXX01",
            "El-A" => "CUGXX000000001",
            "ElC2" => "D1TXX000000001",
            "ElGM" => "CVZXX000000001",
            "El-M" => "CB7XX000000001",
            _ => throw new ArgumentOutOfRangeException(nameof(methodName), methodName, "Unknown method name.")
        };
    }

    private static string GetDefaultSampleCarrierBarcode(string methodName)
    {
        return methodName switch
        {
            "sIgE" => "409XX01",
            "tIgE" => GetDefaultCarrierBarcode(methodName),
            "El-G" => "CU5XX0001",
            "El-T" => GetDefaultCarrierBarcode(methodName),
            "View" => "DHCXXF0",
            "ECP" => "866XX01",
            "Tryp" => "A1CXX01",
            "sIgG" => "E4EXX01",
            "IgG4" => "EERXX01",
            "El-A" => "CJZXX0001",
            "ElC2" => "D1VXX0001",
            "ElGM" => "CVXXX0001",
            "El-M" => "CJYXX0001",
            _ => throw new ArgumentOutOfRangeException(nameof(methodName), methodName, "Unknown method name.")
        };
    }
}
