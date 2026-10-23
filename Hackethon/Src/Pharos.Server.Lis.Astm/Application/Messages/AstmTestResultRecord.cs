using System.Globalization;

namespace Pharos.Server.Lis.Astm.Application.Messages;


internal enum AstmTestResultStatus
{
    None,
    Final,
    FinalWithManualTests,
    Pending,
    Error
}

internal enum AstmPendingTestResultStatus
{
    None, // 
    TestNotCompletedQNS, // "Q"
    TestNotCompletedMissingCAP, // "M"
    TestNotCompletedMissingCAPEsoteric, // "E"
    TestNotCompletedNotStarted, // "N"
    TestNotCompletedProcessing // "P"
}

internal enum AstmErrorTestResultStatus
{
    None, // 
    ProcessingErrorSample, // "S"
    ProcessingErrorReagent, // "R"
    ProcessingErrorInstrument // "I"
}

internal sealed class AstmTestResultRecord : IAstmRecord
{
    public char RecordType => AstmRecordType.Result;
    public int SequenceNumber { get; set; } = 1;
    public string Class { get; set; } = "";
    public string ClinRelation { get; set; } = "";
    public string Concentration { get; set; } = "";
    public string Cutoff { get; set; } = "";
    public string Cutoff2 { get; set; } = "";
    public DateTimeOffset? DateTimeCompleted { get; set; }
    public string InstrumentId { get; set; } = "";
    public AstmTestResultStatus ResultStatus { get; set; }
    public AstmPendingTestResultStatus PendingResultStatus { get; set; }
    public AstmErrorTestResultStatus ProcessingErrorStatus { get; set; }
    public string Quotient { get; set; } = "";
    public string ResultInterpretationCommentIndex { get; set; } = "";
    public string ResultOperatorId { get; set; } = "";
    public string Sensitization { get; set; } = "";
    public AstmTestId? UniversalTestId { get; set; }
    public string Units { get; set; } = "";
    public IList<string> ParsingErrors { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets a value indicating whether to use four fields instead of five fields when building measurement values in result reporting.
    /// When true, the Cutoff2 field is excluded from the DataOrMeasurementValue field, resulting in a four-field format.
    /// When false, all five fields including Cutoff2 are included in the DataOrMeasurementValue field.
    /// </summary>
    internal bool UseFourFieldsInResultReporting { get; set; }

    public string ToRecordString(AstmFieldDelimiters delimiters, bool redactSensitiveData = false)
    {
        var rb = new AstmRecordBuilder<ResultRecordField>(delimiters);

        rb.AddField(ResultRecordField.RecordTypeId, RecordType.ToString());
        rb.AddField(ResultRecordField.SequenceNumber, SequenceNumber.ToString(CultureInfo.InvariantCulture));
        rb.AddField(ResultRecordField.UniversalTestId, "", "", "", UniversalTestId?.TestName ?? "", UniversalTestId?.LisMethodId ?? "", UniversalTestId?.InstrumentDilution.ToString(CultureInfo.InvariantCulture) ?? "");
        rb.AddField(ResultRecordField.DataOrMeasurementValue, BuildMeasurementValues().ToArray());
        rb.AddField(ResultRecordField.Units, Units);
        rb.AddField(ResultRecordField.ResultStatus, MapAstmResultStatus().ToArray());
        rb.AddField(ResultRecordField.OperatorId, ResultOperatorId);
        rb.AddField(ResultRecordField.DateTimeTestCompleted, DateTimeCompleted?.ToString(AstmFieldParser.DateTimeFormat, CultureInfo.InvariantCulture) ?? "");
        rb.AddField(ResultRecordField.InstrumentId, InstrumentId);

        return rb.ToRecordString();
    }

    private IEnumerable<string> MapAstmResultStatus()
    {
        switch (ResultStatus)
        {
            case AstmTestResultStatus.Final:
                yield return "F";
                break;
            case AstmTestResultStatus.Pending:
                yield return "I";

                if (PendingResultStatus != AstmPendingTestResultStatus.None)
                {
                    yield return MapAstmPendingResultStatus(PendingResultStatus);
                }

                break;
            case AstmTestResultStatus.Error:
                yield return "X";

                if (ProcessingErrorStatus != AstmErrorTestResultStatus.None)
                {
                    yield return MapAstmPendingResultStatusError(ProcessingErrorStatus);
                }
                break;
            case AstmTestResultStatus.FinalWithManualTests:
                yield return "M";
                break;
            default:
                yield return "";
                break;
        }
    }

    private List<IAstmRecordField> BuildMeasurementValues()
    {
        var values = new List<string>
        {
            Concentration,
            Class,
            Cutoff
        };

        if (!UseFourFieldsInResultReporting)
        {
            values.Add(Cutoff2);
        }

        values.Add(Quotient.Replace("%", " ").Trim());

        if (values.All(string.IsNullOrWhiteSpace))
        {
            return [];
        }

        var extendedValues = new[]
        {
            Sensitization,
            ClinRelation,
            ResultInterpretationCommentIndex
        };

        if (extendedValues.Any(value => !string.IsNullOrWhiteSpace(value)))
        {
            values.AddRange(extendedValues);
        }

        return values.Select(value => (IAstmRecordField)new StringRecordField(value)).ToList();
    }

    private static string MapAstmPendingResultStatus(AstmPendingTestResultStatus status)
    {
        return status switch
        {
            AstmPendingTestResultStatus.TestNotCompletedQNS => "Q",
            AstmPendingTestResultStatus.TestNotCompletedMissingCAP => "M",
            AstmPendingTestResultStatus.TestNotCompletedMissingCAPEsoteric => "E",
            AstmPendingTestResultStatus.TestNotCompletedNotStarted => "N",
            AstmPendingTestResultStatus.TestNotCompletedProcessing => "P",
            _ => ""
        };
    }

    private static string MapAstmPendingResultStatusError(AstmErrorTestResultStatus error)
    {
        return error switch
        {
            AstmErrorTestResultStatus.ProcessingErrorSample => "S",
            AstmErrorTestResultStatus.ProcessingErrorReagent => "R",
            AstmErrorTestResultStatus.ProcessingErrorInstrument => "I",
            _ => ""
        };
    }

    private enum ResultRecordField
    {
        RecordTypeId,
        SequenceNumber,
        UniversalTestId,
        DataOrMeasurementValue,
        Units,
        ReferenceRanges,
        ResultAbnormalFlags,
        NatureOfAbnormalityTesting,
        ResultStatus,
        DateOfChangeInInstrumentValues,
        OperatorId,
        DateTimeTestStarted,
        DateTimeTestCompleted,
        InstrumentId
    }
}