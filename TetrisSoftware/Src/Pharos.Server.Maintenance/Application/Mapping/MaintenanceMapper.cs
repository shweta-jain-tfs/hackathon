using Pharos.Server.Maintenance.Application.Queries;
using Pharos.Server.Maintenance.Domain;
using Riok.Mapperly.Abstractions;

namespace Pharos.Server.Maintenance.Application.Mapping;

[Mapper]
internal static partial class MaintenanceMapper
{

    public static partial IQueryable<MaintenanceActivityDto> ProjectToMaintenanceActivityDto(this IQueryable<MaintenanceActivity> source);

    [MapperIgnoreTarget(nameof(MaintenanceActivityDto.Status))]
    public static partial MaintenanceActivityDto ToMaintenanceActivityDto(this MaintenanceActivity source);
}
