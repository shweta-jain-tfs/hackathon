using Pharos.Server.Parameters.Application.Queries.GetMethodsAndTest;

namespace Pharos.Server.Parameters.Application.Queries.GetPanelTests;

public class PanelTestDto
{
    public int Id { get; set; }
    public MethodTestDto? MethodTest { get; set; }
}