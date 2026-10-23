using System.Security.Cryptography;
using MediatR;

namespace Pharos.Server.Maintenance.Application.Queries;

public class BottleStateDto
{
    public bool IsInPosition { get; set; }
    public bool IsLiquidLevelOk { get; set; }
}

internal record GetMaintenanceBottleStateQuery(string InstrumentNumber) : IRequest<BottleStateDto>;

internal class GetMaintenanceBottleStateQueryHandler : IRequestHandler<GetMaintenanceBottleStateQuery, BottleStateDto>
{
    public Task<BottleStateDto> Handle(GetMaintenanceBottleStateQuery request, CancellationToken cancellationToken)
    {
        var isInPosition = RandomNumberGenerator.GetInt32(0, 2) == 1;
        var isLiquidLevelOk = isInPosition && RandomNumberGenerator.GetInt32(0, 2) == 1;
        return Task.FromResult(new BottleStateDto
        {
            IsInPosition = isInPosition,
            IsLiquidLevelOk = isLiquidLevelOk
        });
    }
}