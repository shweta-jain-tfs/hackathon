using Pharos.Server.Parameters.Application.Queries.GetPanelTests;

namespace Pharos.Server.Parameters.Application.Queries.GetPanels;

public class PanelDto
{
    public int Id { get; set; }
    public string PanelName { get; set; } = string.Empty;
    public string PanelLongName { get; set; } = string.Empty;

    public List<PanelTestDto> PanelTests { get; set; } = new List<PanelTestDto>();
}