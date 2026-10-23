using Pharos.Common.Web.Components.Drawer;
using Riok.Mapperly.Abstractions;

namespace Pharos.Client.App.Sample;

[Mapper]
public static partial class SampleMapper
{
    [MapProperty(nameof(SampleTrayPosition.SampleTubeStatus), nameof(SampleTube.Status))]
    [MapProperty(nameof(SampleTrayPosition.Error), nameof(SampleTube.TubeError))]
    [MapProperty(nameof(SampleTrayPosition.IsHighPrioritySet), nameof(SampleTube.IsHighPriority))]
    [MapProperty(nameof(SampleTrayPosition.SamplePositionSource), nameof(SampleTube.PositionSource))]
    public static partial SampleTube ToSampleTube(this SampleTrayPosition source);

    public static partial List<SampleTube>? ToSampleTubes(this IEnumerable<SampleTrayPosition>? source);

    [MapEnumValue(SampleTubeStatus.None, TubeStatus.Empty)]
    public static partial TubeStatus ToTubeStatus(this SampleTubeStatus source);

    public static partial TubeError ToTubeError(this SampleTubeError source);

    [MapEnumValue(SampleTubeIdentitySource.BarcodeScanning, TubePositionSource.Instrument)]
    [MapEnumValue(SampleTubeIdentitySource.External, TubePositionSource.LisEntry)]
    [MapperIgnoreTargetValue(TubePositionSource.Unspecified)]
    private static partial TubePositionSource ToTubePositionSource(this SampleTubeIdentitySource source);
}
