using Pharos.Server.Application.Sample;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Sample.Domain;
using Riok.Mapperly.Abstractions;

namespace Pharos.Server.Sample.Application.Mapping;

[Mapper]
internal static partial class SampleTubeMapper
{
    [MapProperty(nameof(SampleTube.SampleIdSuffix), nameof(SampleTubeHistoryDto.DisplayTubeId))]
    [MapProperty(nameof(SampleTube.Status), nameof(SampleTubeHistoryDto.TubeStatus))]
    [MapProperty(nameof(SampleTube.TubePosition), nameof(SampleTubeHistoryDto.TubePositionDisplayValue), Use = nameof(ToCoordinate))]
    [MapProperty(nameof(SampleTube.IdentitySource), nameof(SampleTubeHistoryDto.ManuallyEntered), Use = nameof(ToManuallyEntered))]
    [MapperIgnoreTarget(nameof(SampleTubeHistoryDto.InstrumentName))]
    [MapperIgnoreTarget(nameof(SampleTubeHistoryDto.IsInCurrentInstrument))]
    [MapperIgnoreTarget(nameof(SampleTubeHistoryDto.DoesTrayExist))]
    public static partial SampleTubeHistoryDto ToSampleTubeHistoryDto(this SampleTube source);

    public static partial List<SampleTubeHistoryDto> ToSampleTubeHistoryDtos(this IEnumerable<SampleTube> source);

    [MapProperty(nameof(SampleTube.TubeId), nameof(UnloadableQualityControlTubeDto.Id))]
    [MapProperty(nameof(SampleTube.LoadTimestamp), nameof(UnloadableQualityControlTubeDto.DetectedDateTime), Use = nameof(ToDetectedDateTime))]
    [MapProperty(nameof(SampleTube.Status), nameof(UnloadableQualityControlTubeDto.TubeStatus))]
    [MapProperty(nameof(SampleTube.SampleVolume), nameof(UnloadableQualityControlTubeDto.RemainingVolume))]
    [MapProperty(nameof(SampleTube.TubePosition), nameof(UnloadableQualityControlTubeDto.TubePositionDisplayValue), Use = nameof(ToCoordinate))]
    [MapProperty(nameof(SampleTube.IdentitySource), nameof(UnloadableQualityControlTubeDto.ManuallyEntered), Use = nameof(ToManuallyEntered))]
    [MapperIgnoreTarget(nameof(UnloadableQualityControlTubeDto.Lot))]
    [MapperIgnoreTarget(nameof(UnloadableQualityControlTubeDto.InstrumentName))]
    [MapperIgnoreTarget(nameof(UnloadableQualityControlTubeDto.IsInCurrentInstrument))]
    [MapperIgnoreTarget(nameof(UnloadableQualityControlTubeDto.DoesTrayExist))]
    public static partial UnloadableQualityControlTubeDto ToUnloadableQualityControlTubeDto(this SampleTube source);

    [MapProperty(nameof(SampleTube.TubePosition), nameof(CurrentSampleTubeDto.TubePositionDisplayValue), Use = nameof(ToCoordinate))]
    private static partial CurrentSampleTubeDto ToCurrentSampleTubeDto(this SampleTube source);

    public static partial List<CurrentSampleTubeDto> ToCurrentSampleTubeDtos(this IEnumerable<SampleTube> source);

    private static string? ToCoordinate(int? source)
    {
        if (source is null or < 1 or > 50)
        {
            return null;
        }

        var position = source.Value - 1;
        var row = position / 10;
        var col = (position % 10) + 1;
        var rowChar = (char)('A' + row);

        return $"{rowChar}{col}";
    }

    private static bool ToManuallyEntered(SampleTubeIdentitySource source)
    {
        return source == SampleTubeIdentitySource.ManualInput;
    }

    private static DateTimeOffset ToDetectedDateTime(DateTimeOffset? source)
    {
        return source.GetValueOrDefault();
    }
}
