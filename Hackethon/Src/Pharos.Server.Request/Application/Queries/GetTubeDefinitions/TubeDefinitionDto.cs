namespace Pharos.Server.Request.Application.Queries.GetTubeDefinitions;

public class TubeDefinitionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public bool SupportsVolumeCalculation { get; set; }
}