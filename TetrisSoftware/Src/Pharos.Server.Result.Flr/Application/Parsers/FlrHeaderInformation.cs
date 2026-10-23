namespace Pharos.Server.Result.Flr.Application.Parsers;

internal class FlrHeaderInformation
{
    private const string _dateTimeKey = "DateTime=";
    private const string _formatVersionKey = "FormatVersion=";
    private const string _fileNameKey = "FileName=";
    private const string _instrumentModelKey = "InstrumentModel=";
    private const string _instrumentSerialKey = "InstrumentSerial=";
    private const string _instrumentSoftwareVersionKey = "InstrumentSoftwareVersion=";
    private const string _lastMonthlyMaintenanceKey = "LastMonthlyMaint=";
    private const string _lastWeeklyMaintenanceKey = "LastWeeklyMaint=";
    private const string _assayRunIdKey = "AssayRunId=";

    public string DateTime = string.Empty;
    public string FormatVersion = string.Empty;
    public string FileName = string.Empty;
    public string InstrumentModel = string.Empty;
    public string InstrumentSerial = string.Empty;
    public string InstrumentSoftwareVersion = string.Empty;
    public string LastMonthlyMaintenance = string.Empty;
    public string LastWeeklyMaintenance = string.Empty;
    public string AssayRunId = string.Empty;

    public void ParseHeaderLine(string l, char delimiter)
    {
        var columns = l
            .Replace("\"", "")
            .Replace("“", "")
            .Replace("”", "")
            .Split(delimiter);

        if (columns.Length <= 0)
        {
            return;
        }

        var column = columns[0];

        if (GetValueFromKey(column, _dateTimeKey, ref DateTime)) { }
        else if (GetValueFromKey(column, _formatVersionKey, ref FormatVersion)) { }
        else if (GetValueFromKey(column, _fileNameKey, ref FileName)) { }
        else if (GetValueFromKey(column, _instrumentModelKey, ref InstrumentModel)) { }
        else if (GetValueFromKey(column, _instrumentSerialKey, ref InstrumentSerial)) { }
        else if (GetValueFromKey(column, _instrumentSoftwareVersionKey, ref InstrumentSoftwareVersion)) { }
        else if (GetValueFromKey(column, _lastMonthlyMaintenanceKey, ref LastMonthlyMaintenance)) { }
        else if (GetValueFromKey(column, _lastWeeklyMaintenanceKey, ref LastWeeklyMaintenance)) { }
        else if (GetValueFromKey(column, _assayRunIdKey, ref AssayRunId)) { }
        else
        {
            throw new InvalidDataException("Unknown FLR file header information: " + column);
        }
    }

    private static bool GetValueFromKey(string column, string key, ref string value)
    {
        if (column.StartsWith(key))
        {
            value = column[key.Length..];
            return true;
        }

        return false;
    }

}
