using MediatR;
namespace Pharos.Server.Application.Parameters;

public record GetPanelsForTestsQuery(HashSet<(string MethodName, string TestName)> MethodTests) : IRequest<List<PanelForRecognitionDto>>;

public record PanelForRecognitionDto
{
    public int Id { get; init; }
    public string PanelName { get; init; } = string.Empty;
    public List<(string MethodName, string TestName)> Tests { get; init; } = new();
}
