using Pharos.Server.Application.Reagents;
using Pharos.Server.Application.Sample;

namespace Pharos.Server.Parameters.Application.Queries.QualityControl;

public class QualityControlDto
{
    public int Id { get; set; }
    public bool IsUserDefined { get; set; }
    public string Name { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public bool IsQcInTube { get; set; }
    public List<QualityControlLotDto> Lots { get; set; } = new();
    public List<UnloadableQualityControlCartridgeDto> Cartridges { get; set; } = new();
    public List<UnloadableQualityControlTubeDto> Tubes { get; set; } = new();
}