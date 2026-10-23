using Google.Protobuf.WellKnownTypes;
using Riok.Mapperly.Abstractions;
using Tetris.Protocol.Pharos;
using DomainInstrumentState = Pharos.Server.Domain.Enums.InstrumentState;
using ProtoInstrumentState = Tetris.Protocol.Pharos.InstrumentState;

namespace Pharos.Server.Instrument.Application.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
internal static partial class InstrumentStateMapper
{
    [MapperIgnoreSource(nameof(ProtoInstrumentState.TimeWhenNextStateReached))]
    private static partial void MapStateCoreToInstrument(this ProtoInstrumentState source, Domain.Instrument target);

    public static void MapStateToInstrument(this ProtoInstrumentState source, Domain.Instrument target)
    {
        var shouldUpdateTimeWhenNextStateReached = Domain.Instrument.IsValidStateForStateTimeMessage(source.State.ToDomainInstrumentState());

        source.MapStateCoreToInstrument(target);

        if (shouldUpdateTimeWhenNextStateReached)
        {
            target.TimeWhenNextStateReached = source.TimeWhenNextStateReached.ToDateTimeOffsetNullable();
        }
    }

    [MapperIgnoreSourceValue(EnumInstrumentState.UnspecifiedInstrumentState)]
    [MapperIgnoreTargetValue(DomainInstrumentState.Offline)]
    [MapperIgnoreTargetValue(DomainInstrumentState.Limited)]
    [MapperIgnoreTargetValue(DomainInstrumentState.Synchronizing)]
    [MapEnum(EnumMappingStrategy.ByName, FallbackValue = DomainInstrumentState.Standby)]
    public static partial DomainInstrumentState ToDomainInstrumentState(this EnumInstrumentState source);

    private static Guid ToGuid(this string? source)
    {
        return Guid.TryParse(source, out var guid) ? guid : Guid.Empty;
    }

    private static DateTimeOffset? ToDateTimeOffsetNullable(this Timestamp? source)
    {
        return source?.ToDateTimeOffset().ToLocalTime();
    }
}
