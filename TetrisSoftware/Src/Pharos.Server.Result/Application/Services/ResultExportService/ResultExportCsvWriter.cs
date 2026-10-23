using System.Text;

namespace Pharos.Server.Result.Application.Services.ResultExportService;

public static class ResultExportCsvWriter
{
    private const string _fileHeader = "sep=;\r\n[FILE]\r\nType=ASSAY RESULT DATA FILE\r\nFormatVersion=1.0";
    private const string _informationHeader = "[INFORMATION]";
    private const string _resultHeader = "[RESULTS]";
    private const string _meanHeader = "[MEAN]";

    public static string WriteCsv(CsvInformation information, IEnumerable<CsvResult> result, IEnumerable<CsvMean> mean,
        char delimiter = ';')
    {
        var csvWriter = new CsvWriter(delimiter);
        var res = new StringBuilder();
        _ = res.AppendLine(_fileHeader);
        _ = res.AppendLine();
        _ = res.AppendLine(_informationHeader);
        _ = res.Append(csvWriter.GenerateCsv(Enumerable.Repeat(information, 1)));
        _ = res.AppendLine();
        _ = res.AppendLine(_resultHeader);
        _ = res.Append(csvWriter.GenerateCsv(result));
        _ = res.AppendLine();
        _ = res.AppendLine(_meanHeader);
        _ = res.Append(csvWriter.GenerateCsv(mean));

        return res.ToString();
    }
}