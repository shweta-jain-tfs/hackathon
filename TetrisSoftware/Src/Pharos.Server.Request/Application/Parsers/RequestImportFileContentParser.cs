using Pharos.Server.Request.Application.Interfaces;
using Pharos.Server.Request.Application.Models;
using System.Globalization;

namespace Pharos.Server.Request.Application.Parsers;

internal record HeaderColumn(string Name, bool Mandatory, Action<ImportedSample, ImportedTest, string> UpdateData);

internal class RequestImportFileContentParser : IRequestImportFileContentParser
{
    private const string _customRunIdField = "CustomRunId:";

    public ImportedRequests Parse(string content, string fileName)
    {
        SplitContents(content, out var customRunId, out var headerRow, out var requestRows);
        var requests = ParseContents(headerRow, requestRows, customRunId);

        return requests;
    }

    private static void SplitContents(string content, out string customRunId, out string headerRow, out List<string> requestRows)
    {
        customRunId = string.Empty;
        var readHeaders = false;
        headerRow = string.Empty;
        requestRows = new List<string>();
        var lines = content.Split(Environment.NewLine);

        foreach (var l in lines)
        {
            var line = l.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith(';'))
            {
                continue;
            }
            else if (line.StartsWith(_customRunIdField))
            {
                customRunId = line[_customRunIdField.Length..];
            }
            else if (!readHeaders)
            {
                headerRow = line;
                readHeaders = true;
            }
            else
            {
                requestRows.Add(line);
            }
        }
    }

    private static ImportedRequests ParseContents(string headerRow, List<string> requestRows, string? customRunId)
    {

        var columnDivider = ",";
        if (headerRow.Contains(';'))
        {
            columnDivider = ";";
        }

        var headerColumns = ParseHeader(headerRow, columnDivider);
        var requests = ParseRequestRows(headerColumns, requestRows, columnDivider);
        requests.CustomRunId = customRunId;
        if (requests.CustomRunId != null)
        {
            requests.CustomRunId = requests.CustomRunId.TrimEnd(columnDivider[0]);
        }

        return requests;
    }

    private static List<HeaderColumn> ParseHeader(string headerRow, string columnDivider)
    {
        List<HeaderColumn> definedColumns = new()
        {
            new HeaderColumn("Not defined", false, (s, t, columnValue) => { }),

            new HeaderColumn("SampleId", true, (s, t, columnValue) => s.SampleId = columnValue),
            new HeaderColumn("MethodName", true, (s, t, columnValue) => t.MethodName = columnValue),
            new HeaderColumn("TestName", true, (s, t, columnValue) => t.TestName = columnValue),
            new HeaderColumn("SampleDate", false, (s, t, columnValue) => s.SampleDate = string.IsNullOrEmpty(columnValue) ? null : GetSampleDate(columnValue)),
            new HeaderColumn("ReadBarcode", false, (s, t, columnValue) => s.ReadBarcode = columnValue != "0"),
            new HeaderColumn("TrayId", false, (s, t, columnValue) => s.TrayId = columnValue),
            new HeaderColumn("TubePosition", false, (s, t, columnValue) => s.TubePosition = columnValue),
            new HeaderColumn("TubeType", false, (s, t, columnValue) => s.TubeType = GetInt(columnValue)),
            new HeaderColumn("Replicates", false, (s, t, columnValue) => t.Replicates = GetInt(columnValue)),
            new HeaderColumn("PreDilutionFactor", false, (s, t, columnValue) => s.PreDilutionFactor = GetInt(columnValue)),
            new HeaderColumn("DilutionFactor", false, (s, t, columnValue) => t.InstrumentDilutionFactor = GetInt(columnValue)),
            new HeaderColumn("RequestId", false, (s, t, columnValue) => s.RequestId = columnValue),
            new HeaderColumn("RequestorId", false, (s, t, columnValue) => s.RequestorId = columnValue),
            new HeaderColumn("CarrierLot", false, (s, t, columnValue) => t.CarrierLot = columnValue),
            new HeaderColumn("DiluentLot", false, (s, t, columnValue) => t.DiluentLot = columnValue),
            new HeaderColumn("ConjugateLot", false, (s, t, columnValue) => t.ConjugateLot = columnValue),
            new HeaderColumn("DevelopmentLot", false, (s, t, columnValue) => t.DevelopmentLot = columnValue),
            new HeaderColumn("CalibratorCartridgeLot", false, (s, t, columnValue) => t.CalibratorCartridgeLot = columnValue),
            new HeaderColumn("CalibratorCarrierLot", false, (s, t, columnValue) => t.CalibratorCarrierLot = columnValue),
            new HeaderColumn("CalibratorConjugateLot", false, (s, t, columnValue) => t.CalibratorConjugateLot = columnValue),
            new HeaderColumn("TestOrderId", false, (s, t, columnValue) => t.CustomTestId = columnValue), // Legacy...
            new HeaderColumn("CustomTestId", false, (s, t, columnValue) => t.CustomTestId = columnValue),
        };

        var headerColumns = headerRow.Split(columnDivider);
        var columnList = new List<HeaderColumn>();
        var notDefinedHeader = definedColumns.First(h => h.Name == "Not defined");

        foreach (var headerColumnText in headerColumns)
        {
            var header = definedColumns.FirstOrDefault(h => h.Name == headerColumnText);
            if (header != null)
            {
                columnList.Add(header);
            }
            else
            {
                columnList.Add(notDefinedHeader);
                throw new ArgumentException($"Request import file included unknown column '{headerColumnText}' (headerColumnText).", nameof(headerRow));
            }
        }

        // Check all mandatory columns included
        foreach (var mandatoryColumnName in definedColumns.Where(c => c.Mandatory).Select(c => c.Name).ToList())
        {
            if (!columnList.Any(c => c.Name == mandatoryColumnName))
            {
                throw new ArgumentException($"Request import file missing mandatory column '{mandatoryColumnName}' (mandatoryColumnName).", nameof(headerRow));
            }
        }

        return columnList;
    }

    private static int? GetInt(string columnValue)
    {
        if (!string.IsNullOrEmpty(columnValue))
        {
            if (int.TryParse(columnValue, out var intValue))
            {
                return intValue;
            }
        }

        return null;
    }

    private static DateTimeOffset? GetSampleDate(string columnValue)
    {
        if (string.IsNullOrEmpty(columnValue))
        {
            return null;
        }

        if (DateTime.TryParseExact(
            columnValue,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var sampleDate))
        {
            return sampleDate;
        }

        throw new FormatException($"Invalid sample date '{columnValue}'. Expected format: yyyy-MM-dd.");
    }

    private static ImportedRequests ParseRequestRows(List<HeaderColumn> headerColumns, List<string> requestRows, string columnDivider)
    {
        var request = new ImportedRequests();

        for (var row = 0; row < requestRows.Count; row++)
        {
            var requestRow = requestRows[row];
            var sample = new ImportedSample();
            var test = new ImportedTest();

            var rowColumns = requestRow.Split(columnDivider);
            if (rowColumns.Length != headerColumns.Count)
            {
                throw new ArgumentException(
                    $"Request import file row {row + 1} has {rowColumns.Length} column(s), expected {headerColumns.Count} (requestRow).",
                    nameof(requestRows));
            }

            for (var col = 0; col < headerColumns.Count; col++)
            {
                try
                {
                    headerColumns[col].UpdateData(sample, test, rowColumns[col]);
                }
                catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException or FormatException)
                {
                    throw new ArgumentException(
                        $"Invalid value at row {row + 1}, column {col + 1} ('{headerColumns[col].Name}').",
                        nameof(requestRows),
                        ex);
                }
            }

            var existingSample = request.Samples.FirstOrDefault(s => s.SampleId == sample.SampleId);
            if (existingSample != null)
            {
                existingSample.Tests.Add(test);
            }
            else
            {
                request.Samples.Add(sample);
                sample.Tests.Add(test);
            }
        }

        return request;
    }
}
