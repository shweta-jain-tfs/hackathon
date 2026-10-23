namespace Pharos.Server.Parameters.Application.Commands.Panels;

public class MethodPanelTestsDto
{
    public int Id { get; set; }
    public string MethodName { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public int Replicates { get; set; }
    public int InstrumentDilutionFactor { get; set; }
    public int SortOrder { get; set; }
}
