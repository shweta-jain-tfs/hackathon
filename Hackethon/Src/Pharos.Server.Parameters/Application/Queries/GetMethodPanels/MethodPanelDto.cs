namespace Pharos.Server.Parameters.Application.Queries.GetMethodPanels;

public class MethodPanelDto
{
    public int Id { get; set; }
    public string PanelName { get; set; } = string.Empty;
    public string PanelLongName { get; set; } = string.Empty;
    public bool Grouped { get; set; }
    public bool Enabled { get; set; }
}
