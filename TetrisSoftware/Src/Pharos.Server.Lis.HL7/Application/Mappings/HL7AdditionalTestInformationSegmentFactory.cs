using Pharos.Server.Application.Lis;
using Pharos.Server.Lis.HL7.Application.Messages;

namespace Pharos.Server.Lis.HL7.Application.Mappings;

internal static class HL7AdditionalTestInformationSegmentFactory
{
    private const string _concentrationObservationPrefix = "CONC";
    private const string _concentrationObservationText = "Concentration";

    public static IReadOnlyList<HL7AdditionalTestInformationSegment> CreateFromTestOrderAndResult(
        LisTestOrder testOrder,
        int firstSequenceNumber,
        bool forceUniqueLaboratoryTestNames,
        bool isSampleLocallyCreated)
    {
        var result = testOrder.TestResult!;
        var resultStatus = MapResultStatus(result, isSampleLocallyCreated);
        var methodTestIdentifier = CreateMethodTestIdentifier(testOrder.MethodName, testOrder.TestName, forceUniqueLaboratoryTestNames);
        var observationText = CreateObservationText(testOrder);

        var segments = new List<HL7AdditionalTestInformationSegment>();
        var sequenceNumber = firstSequenceNumber;

        if (testOrder.ReportConcentration)
        {
            segments.Add(CreateResultSegment(sequenceNumber++, $"CONC-{methodTestIdentifier}", observationText, NormalizeDecimalSeparator(result.Concentration), result.Units ?? "", resultStatus, result.MeasurementDateTime));
        }

        if (testOrder.ReportClass)
        {
            segments.Add(CreateResultSegment(sequenceNumber++, $"CLASS-{methodTestIdentifier}", observationText, result.Class ?? "", "", resultStatus, result.MeasurementDateTime));
        }

        if (testOrder.ReportCutOff)
        {
            segments.Add(CreateResultSegment(sequenceNumber++, $"CUTOFF-{methodTestIdentifier}", observationText, result.CutOff ?? "", "", resultStatus, result.MeasurementDateTime));
        }

        if (testOrder.ReportQuotient)
        {
            segments.Add(CreateResultSegment(sequenceNumber, $"QUOT-{methodTestIdentifier}", observationText, NormalizeDecimalSeparator(result.Quotient), "", resultStatus, result.MeasurementDateTime));
        }

        return segments;
    }

    public static HL7AdditionalTestInformationSegment CreateFromQualityControlResult(LisQualityControlTestResult qcTestResult)
    {
        var resultStatus = qcTestResult.Concentration == null
            ? HL7ObservationResultStatus.Canceled
            : HL7ObservationResultStatus.Final;

        var observationId = CreateQCObservationId(qcTestResult);

        return CreateResultSegment(
            sequenceNumber: 1,
            observationId,
            observationText: _concentrationObservationText,
            NormalizeDecimalSeparator(qcTestResult.Concentration),
            qcTestResult.Units ?? "",
            resultStatus,
            qcTestResult.MeasurementDateTime);
    }

    private static HL7AdditionalTestInformationSegment CreateResultSegment(
        int sequenceNumber,
        string observationId,
        string observationText,
        string observationValue,
        string units,
        HL7ObservationResultStatus resultStatus,
        DateTimeOffset? observationDateTime)
    {
        return new HL7AdditionalTestInformationSegment
        {
            SequenceNumber = sequenceNumber,
            ObservationId = observationId,
            ObservationText = observationText,
            ObservationValue = observationValue,
            Units = units,
            ResultStatus = resultStatus,
            ObservationDateTime = observationDateTime
        };
    }

    private static HL7ObservationResultStatus MapResultStatus(
        LisSampleTestResult result,
        bool isSampleLocallyCreated)
    {
        if (result.HasError)
        {
            return HL7ObservationResultStatus.Canceled;
        }

        if (result.Pending)
        {
            return HL7ObservationResultStatus.Pending;
        }

        return isSampleLocallyCreated
            ? HL7ObservationResultStatus.FinalWithManualTests
            : HL7ObservationResultStatus.Final;
    }

    private static string NormalizeDecimalSeparator(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "" : value.Replace(",", ".");
    }

    private static string CreateQCObservationId(LisQualityControlTestResult qcTestResult)
    {
        return string.Join('-', new[]
        {
            _concentrationObservationPrefix,
            qcTestResult.MethodName,
            qcTestResult.TestName
        }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string CreateMethodTestIdentifier(string methodName, string testName, bool forceUniqueLaboratoryTestNames)
    {
        if (forceUniqueLaboratoryTestNames)
        {
            return testName;
        }

        return string.Join('-', new[] { methodName, testName }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string CreateObservationText(LisTestOrder testOrder)
        => $"{testOrder.FullTestName} ({testOrder.TestName})";
}
