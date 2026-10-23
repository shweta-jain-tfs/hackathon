using MediatR;
using Microsoft.EntityFrameworkCore;
using Pharos.Server.Maintenance.Application.Interfaces;
using Pharos.Server.Maintenance.Application.Mapping;
using Pharos.Server.Maintenance.Domain;

namespace Pharos.Server.Maintenance.Application.Queries;

internal record GetMaintenanceActivitiesQuery(string InstrumentNumber) : IRequest<List<MaintenanceActivityDto>>;

public class MaintenanceActivityDto
{
    public int Id { get; set; }
    public MaintenanceActivityType ActivityType { get; set; }
    public MaintenanceScheduledType ScheduledType { get; set; }
    public int Duration { get; set; }
    public DateTimeOffset LastCompleted { get; set; }

    public DateTimeOffset? Next { get; set; }

    public MaintenanceActivityStatus Status { get; set; } = MaintenanceActivityStatus.None;
}

internal class GetMaintenanceActivitiesQueryHandler : IRequestHandler<GetMaintenanceActivitiesQuery, List<MaintenanceActivityDto>>
{
    private readonly IMaintenanceDbContext _dbContext;
    private readonly IMaintenanceDefinitionProvider _definitionProvider;

    public GetMaintenanceActivitiesQueryHandler(IMaintenanceDbContext dbContext, IMaintenanceDefinitionProvider definitionProvider)
    {
        _dbContext = dbContext;
        _definitionProvider = definitionProvider;
    }

    public async Task<List<MaintenanceActivityDto>> Handle(GetMaintenanceActivitiesQuery request, CancellationToken cancellationToken)
    {
        var dtos = await _dbContext.Activities.Where(x => x.InstrumentNumber == request.InstrumentNumber)
            .AsNoTracking()
            .ProjectToMaintenanceActivityDto()
            .ToListAsync(cancellationToken);

        foreach (var dto in dtos)
        {
            dto.Status = _definitionProvider.GetMaintenanceStatus(dto.ActivityType, dto.Next);
        }

        return dtos;
    }
}