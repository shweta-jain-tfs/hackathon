using Google.Protobuf.WellKnownTypes;

namespace Pharos.Server.Result.Flr.Application.Parsers;

internal enum FlrMeasurementType { Unknown = 0, DarkBlank = 1, RinseBlank = 2, WashBlank = 3, ReagentBlank = 4, FluoroC = 5 }

internal abstract class FlrEntry
{
    public string GroupId { get; set; } = string.Empty;
    public FlrMeasurementType MeasurementType { get; set; }
    public string DevelopmentBarcode { get; set; } = string.Empty;
    public int DevelopmentWell { get; set; }
    public string StopBarcode { get; set; } = string.Empty;

    public double MeasurementValue { get; set; }
    public Timestamp MeasurementDateAndTime { get; set; } = null!;


    protected FlrEntry(FlrMeasurementType measurementType)
    {
        MeasurementType = measurementType;
    }

    public static FlrEntry? TryParse(string[] columnValues, Dictionary<string, FlrEntryColumn> columns)
    {
        var measurementType = (FlrMeasurementType)columnValues.GetInt(columns, FlrEntryColumns.MeasurementType);
        FlrEntry result;
        switch (measurementType)
        {
            case FlrMeasurementType.Unknown:
                result = new AssayResultFlrEntry(measurementType);
                break;
            case FlrMeasurementType.ReagentBlank:
            case FlrMeasurementType.RinseBlank:
                result = new BlankResultFlrEntry(measurementType);
                break;
            case FlrMeasurementType.DarkBlank:
            case FlrMeasurementType.WashBlank:
            case FlrMeasurementType.FluoroC:
                return null; // we don't need to process these measurement types, so we can skip them by returning null
            default:
                throw new FormatException($"Unknown measurement type '{measurementType}' in the FLR file record.");
        }
        result.Parse(columnValues, columns);
        return result;
    }

    protected virtual void Parse(string[] columnValues, Dictionary<string, FlrEntryColumn> columns)
    {
        DevelopmentBarcode = columnValues.GetString(columns, FlrEntryColumns.DevelopmentBarcode, canBeEmpty: false);
        DevelopmentWell = columnValues.GetCartridgeCell(columns, FlrEntryColumns.DevelopmentLocation, canBeEmpty: false);
        StopBarcode = columnValues.GetString(columns, FlrEntryColumns.StopBarcode);

        MeasurementDateAndTime = columnValues.GetTimestamp(columns, FlrEntryColumns.MeasurementDateTime) ?? DateTimeOffset.UtcNow.ToTimestamp();
        MeasurementValue = columnValues.GetDouble(columns, FlrEntryColumns.MeasurementValue);
        var fluorometerStatus = columnValues.GetInt(columns, FlrEntryColumns.FluorometerStatus);
        if (fluorometerStatus != 0)
        {
            // the fluorometer status is non-zero, which indicates an error during the measurement.
            // In this case, we set the measurement value to 0 to avoid using erroneous data.
            MeasurementValue = 0;
        }
    }
}
