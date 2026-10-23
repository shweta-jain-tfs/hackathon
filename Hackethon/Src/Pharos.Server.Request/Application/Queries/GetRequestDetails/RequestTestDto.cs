namespace Pharos.Server.Request.Application.Queries.GetRequestDetails;

public class RequestTestDto
{
    public int Id { get; set; }
    public Guid Uid { get; set; }

    public int? AnalyticalRunId { get; set; }
    public string? Class { get; set; } = string.Empty;
    public string? Concentration { get; set; }
    public string? FullMethodName { get; set; } = string.Empty;
    public string? FullTestName { get; set; } = string.Empty;
    public int InstrumentDilutionFactor { get; set; }
    public string? InstrumentName { get; set; }
    public bool IsReset { get; set; }
    public string LabTestName { get; set; } = string.Empty;
    public DateTimeOffset? LastModified { get; set; }
    public string? MeanCutOff { get; set; }
    public string? MeanQuotientAsText { get; set; }
    public DateTimeOffset? MeasurementDateTime { get; set; }
    public string MethodName { get; set; } = string.Empty;
    public int NumberOfReplicates { get; set; }
    public List<RequestReflexDefinitionDto> ReflexDefinitions { get; set; } = [];
    public bool ReportClass { get; set; }
    public bool ReportConcentration { get; set; }
    public bool ReportCutOff { get; set; }
    public bool ReportQuotient { get; set; }
    public TestOrderState State { get; set; }
    public int TestSortOrder { get; set; }
    public List<int> TestPanelIds { get; set; } = [];
    public string TestName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}