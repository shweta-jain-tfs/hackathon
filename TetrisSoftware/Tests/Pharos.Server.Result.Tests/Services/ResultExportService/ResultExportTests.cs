using Microsoft.VisualBasic.FileIO;
using Pharos.Server.Result.Application.Services.ResultExportService;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Services.ResultExportService;
public class ResultExportTests
{
    [Fact]
    public void CreateInformation()
    {
        var info = new CsvInformation
        {
            TypeOfExport = (int)ResultExportType.BySingleAnalyticalRun,
            StartDateTime = DateTimeOffset.Now,
            AnalyticalRunId = "run id",
        };
        var writer = new CsvWriter();
        var res = writer.GenerateCsv(Enumerable.Repeat(info, 1));
        _ = res.Contains("AnalyticalRunId;").Should().BeTrue();
    }

    [Fact]
    public void CreateResults()
    {
        var result = new CsvResult[]
        {
            new() {
                Sequence = 1,
                MeasurementDateTime = DateTimeOffset.Now,
                MethodName = "sIgE",
                SampleId = "sampleid",
                Status = (int)ResultStatus.Measured,
                SampleType = (int)TestResultType.Sample,
            },
            new() {
                Sequence = 2,
                MeasurementDateTime = DateTimeOffset.Now,
                MethodName = "tIgE",
                SampleId = "sampleid",
                Status = (int)ResultStatus.Aborted,
                SampleType = (int)TestResultType.Sample,
            },
        };

        var writer = new CsvWriter();
        var res = writer.GenerateCsv(result);
        res.Contains("Seq;").Should().BeTrue();
    }

    [Fact]
    public void CreateMeans()
    {
        var meanRow = new CsvMean
        {
            SampleType = (int)TestResultType.Sample,
            SampleId = "identifier",
            MethodName = "sIgE",
            TestName = "c1",
            MeanResponse = 1498.5,
            MeanConcentration = 0,
        };

        var mean = new CsvMean[]
        {
            meanRow with { Sequence = 1 },
            meanRow with { Sequence = 2 },
        };

        var writer = new CsvWriter();
        var res = writer.GenerateCsv(mean);
        res.Contains("Seq;").Should().BeTrue();
    }

    [Fact]
    public void CreateCsv()
    {

        var info = new CsvInformation
        {
            TypeOfExport = (int)ResultExportType.BySingleAnalyticalRun,
            StartDateTime = DateTimeOffset.Now,
            AnalyticalRunId = "run id",
        };

        var result = new CsvResult[]
        {
            new() {
                Sequence = 1,
                MeasurementDateTime = DateTimeOffset.Now,
                MethodName = "sIgE",
                SampleId = "sampleid",
                Status = (int)ResultStatus.Measured,
                SampleType = (int)TestResultType.Sample,
            },
            new() {
                Sequence = 2,
                MeasurementDateTime = DateTimeOffset.Now,
                MethodName = "tIgE",
                SampleId = "sampleid",
                Status = (int)ResultStatus.Aborted,
                SampleType = (int)TestResultType.Sample,
            },
        };

        var meanRow = new CsvMean
        {
            SampleType = (int)TestResultType.Sample,
            SampleId = "identifier",
            MethodName = "sIgE",
            TestName = "c1",
            MeanResponse = 1498.5,
            MeanConcentration = 0,
        };

        var mean = new CsvMean[]
        {
            meanRow with { Sequence = 1 },
            meanRow with { Sequence = 2 },
        };

        var res = ResultExportCsvWriter.WriteCsv(info, result, mean);
        _ = IsValidCsv(res).Should().BeTrue();
    }

    private static bool IsValidCsv(string csvContent)
    {
        try
        {
            using var reader = new StringReader(csvContent);
            using var parser = new TextFieldParser(reader);

            parser.TextFieldType = FieldType.Delimited;
            parser.SetDelimiters(";");

            while (!parser.EndOfData)
            {
                _ = parser.ReadFields();
            }

            return true;
        }
        // Swallow Exception, not a valid CSV
        catch (Exception)
        {
            return false;
        }
    }
}
