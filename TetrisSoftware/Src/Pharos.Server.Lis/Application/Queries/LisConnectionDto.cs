using Pharos.Server.Lis.Application.Interfaces;

namespace Pharos.Server.Lis.Application.Queries;

public class LisConnectionDto
{
    public LisConnectionState State { get; set; }
    public int Id { get; set; } //TODO, remove id from this class. Now it is here to make the client happy and enable the use of IEntityModel
}