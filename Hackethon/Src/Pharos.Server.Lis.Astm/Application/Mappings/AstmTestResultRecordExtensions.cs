using Pharos.Server.Application.Lis;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Lis.Astm.Application.Messages;

namespace Pharos.Server.Lis.Astm.Application.Mappings;

internal static class AstmTestResultRecordExtensions
{
    public static void MapFromTestOrderAndResult(this AstmTestResultRecord record, LisTestOrder testOrder, LisSampleTestResult sampleTestResult, bool isLocalSample, AstmExportSettings exportSettings)
    {
        record.UseFourFieldsInResultReporting = exportSettings.UseFourFieldsInResultReporting;

        record.UniversalTestId = new AstmTestId(testOrder.TestName, testOrder.MethodName, testOrder.InstrumentDilutionFactor ?? 1, 0, "", 0);
        record.DateTimeCompleted = sampleTestResult.MeasurementDateTime;
        record.Concentration = testOrder.ReportConcentration
            ? sampleTestResult.Concentration?.Replace(",", ".") ?? string.Empty
            : string.Empty;
        record.Class = testOrder.ReportClass
            ? sampleTestResult.Class ?? string.Empty
            : string.Empty;
        record.Cutoff = testOrder.ReportCutOff
            ? sampleTestResult.CutOff ?? string.Empty
            : string.Empty;
        record.Quotient = testOrder.ReportQuotient
            ? sampleTestResult.Quotient?.Replace(",", ".") ?? string.Empty
            : string.Empty;
        record.Units = testOrder.ReportConcentration
            ? sampleTestResult.Units ?? string.Empty
            : string.Empty;

        record.InstrumentId = exportSettings.OverrideInstrumentName.Length > 0
            ? exportSettings.OverrideInstrumentName
            : sampleTestResult.InstrumentName ?? string.Empty;

        if (exportSettings.IncludeUserIdInResultsRecord)
        {
            record.ResultOperatorId = sampleTestResult.ApprovedByUser ?? string.Empty;
        }

        record.ResultStatus = DetermineResultStatus(sampleTestResult, isLocalSample);
    }

    public static void MapFromQualityControlResult(this AstmTestResultRecord record, LisQualityControlTestResult qcTestResult, AstmExportSettings exportSettings)
    {
        record.UniversalTestId = new AstmTestId(qcTestResult.TestName, qcTestResult.MethodName, qcTestResult.InstrumentDilutionFactor, 0, "", 0);
        record.Concentration = qcTestResult.Concentration?.Replace(",", ".") ?? string.Empty;
        record.Units = qcTestResult.Units ?? string.Empty;

        record.ResultStatus = qcTestResult.Concentration == null
            ? AstmTestResultStatus.Error
            : AstmTestResultStatus.FinalWithManualTests;

        record.DateTimeCompleted = qcTestResult.MeasurementDateTime;
        record.InstrumentId = exportSettings.OverrideInstrumentName.Length > 0
            ? exportSettings.OverrideInstrumentName
            : qcTestResult.InstrumentName ?? string.Empty;
    }

    private static AstmTestResultStatus DetermineResultStatus(LisSampleTestResult sampleTestResult, bool isLocalSample)
    {
        if (sampleTestResult.Pending)
        {
            return AstmTestResultStatus.Pending;
        }
        else if (sampleTestResult.HasError)
        {
            return AstmTestResultStatus.Error;
        }
        else if (sampleTestResult.Concentration != null || sampleTestResult.MeasurementDateTime != null)
        {
            return isLocalSample
                ? AstmTestResultStatus.FinalWithManualTests
                : AstmTestResultStatus.Final;
        }
        else
        {
            throw new InvalidOperationException("Test result is inconclusive.");
        }
    }
}
