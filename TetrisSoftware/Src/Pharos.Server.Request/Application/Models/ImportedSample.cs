namespace Pharos.Server.Request.Application.Models;

internal class ImportedSample
{
    public string SampleId { get; set; } = string.Empty;
    public DateTimeOffset? SampleDate { get; set; }
    public bool ReadBarcode { get; set; } = true;
    public string TrayId { get; set; } = string.Empty;
    public string TubePosition { get; set; } = string.Empty;
    public int? TubeType { get; set; }
    public int? PreDilutionFactor { get; set; }
    public string RequestId { get; set; } = string.Empty;
    public string RequestorId { get; set; } = string.Empty;

    public List<ImportedTest> Tests { get; } = new List<ImportedTest>();
}

