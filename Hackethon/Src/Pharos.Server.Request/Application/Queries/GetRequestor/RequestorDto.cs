using Pharos.Server.Domain.Common;

namespace Pharos.Server.Request.Application.Queries.GetRequestor;

public class RequestorDto
{
    public int Id { get; set; }
    public string? RequestorId { get; set; } = string.Empty;
    [ProtectedData] public string? Name { get; set; } = string.Empty;
    public bool ReflexEnabled { get; set; }
}
