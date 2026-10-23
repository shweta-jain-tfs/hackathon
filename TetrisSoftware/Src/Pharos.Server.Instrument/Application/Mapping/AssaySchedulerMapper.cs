using Pharos.Contracts;
using Pharos.Server.Instrument.Application.Queries.GetAssaySchedulers;
using Pharos.Server.Instrument.Domain;
using Riok.Mapperly.Abstractions;

namespace Pharos.Server.Instrument.Application.Mapping;

[Mapper]
internal static partial class AssaySchedulerMapper
{
    [MapProperty(nameof(@AssayScheduler.Instrument.DisplayName), nameof(AssaySchedulerDto.InstrumentName))]
    [MapProperty(nameof(@AssayScheduler.SchedulerDays), nameof(AssaySchedulerDto.AssaySchedulerDays))]
    public static partial AssaySchedulerDto ToAssaySchedulerDto(this AssayScheduler source);

    public static partial AssaySchedulerDayDto ToAssaySchedulerDayDto(AssaySchedulerDay source);
}
