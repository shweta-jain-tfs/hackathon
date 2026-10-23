using Pharos.Server.Application.Parameters;

namespace Pharos.Server.Parameters.Application.Queries.GetMethodsAndTest;

public class MethodTestDto
{
    public int Id { get; set; }
    public string TestName { get; set; } = string.Empty;
    public string LabTestName { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public string MethodConcentrationUnit { get; set; } = string.Empty;
    public string? FullTestName { get; set; }
    public string DefaultLabTestName { get; set; } = default!;
    public bool Active { get; set; }
    public bool ReportConcentration { get; set; }
    public bool ReportCutOff { get; set; }
    public QuotientReportMode QuotientReportMode { get; set; }
    public bool ReportClass { get; set; }
    public string? LongMethodName { get; set; }
    public int Replicates { get; set; }
    public int DilutionFactor { get; set; }
    public bool IsRuo { get; set; }
    public AssayTechnology Technology { get; set; }
    public List<MethodTestLabNameDto>? MethodTestLabNames { get; set; }
    public bool UsesInstrumentDilution { get; set; }
}