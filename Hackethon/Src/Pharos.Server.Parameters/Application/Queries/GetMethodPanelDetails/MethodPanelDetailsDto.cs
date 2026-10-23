namespace Pharos.Server.Parameters.Application.Queries.GetMethodPanelDetails;

public class MethodPanelDetailsDto
{
    public int Id { get; set; }
    public string PanelName { get; set; } = string.Empty;
    public string PanelLongName { get; set; } = string.Empty;
    public bool Grouped { get; set; }
    public bool Enabled { get; set; }

    public List<MethodPanelTestDto> Tests { get; set; } = default!;
}

public class MethodPanelTestDto
{
    public int Id { get; set; }
    
    /// <summary>
    /// When testing this one, repeat it a number of times
    /// </summary>
    public int Replicates { get; set; }

    public int InstrumentDilutionFactor { get; set; }

    /// <summary>
    /// Listing order of the panel tests
    /// </summary>
    public int? SortOrder { get; set; }

    public string MethodName { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string FullTestName { get; set; } = string.Empty;
    public string FullMethodName { get; set; } = string.Empty;
}
