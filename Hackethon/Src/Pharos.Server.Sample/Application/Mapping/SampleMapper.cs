using Google.Protobuf.WellKnownTypes;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Sample.Domain;
using Riok.Mapperly.Abstractions;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos.Storage;
using InstrumentSampleDrawer = Tetris.Protocol.Pharos.Storage.SampleDrawer;
using InstrumentSampleTray = Tetris.Protocol.Pharos.Storage.SampleTray;
using InstrumentSampleTube = Tetris.Protocol.Pharos.Storage.SampleTube;
using SampleDrawer = Pharos.Server.Sample.Domain.SampleDrawer;
using SampleTray = Pharos.Server.Sample.Domain.SampleTray;
using SampleTube = Pharos.Server.Sample.Domain.SampleTube;

namespace Pharos.Server.Sample.Application.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
internal static partial class SampleMapper
{

    [MapProperty(nameof(InstrumentSampleDrawer.Id), nameof(SampleDrawer.DrawerType))]
    public static partial SampleDrawer ToSampleDrawer(this InstrumentSampleDrawer source);

    public static SampleTube ToSampleTube(this InstrumentSampleTube source)
    {
        var target = new SampleTube();
        source.UpdateSampleTube(target);
        return target;
    }

    public static void UpdateSampleTube(this InstrumentSampleTube source, SampleTube target)
    {
        source.MapInstrumentToSampleTube(target);
        if (source.SampleType == EnumSampleType.QualityControl)
        {
            target.Status = source.SampleQcStatus.ToSampleTubeStatus();
        }
    }

    [MapEnumValue(EnumSampleDrawer.Drawer1, DrawerType.Sample1)]
    [MapEnumValue(EnumSampleDrawer.Drawer2, DrawerType.Sample2)]
    [MapperIgnoreSourceValue(EnumSampleDrawer.UnspecifiedSampleDrawer)]
    [MapperIgnoreTargetValue(DrawerType.BottledReagent)]
    [MapperIgnoreTargetValue(DrawerType.Reagent)]
    [MapperIgnoreTargetValue(DrawerType.RinseWaste)]
    [MapperIgnoreTargetValue(DrawerType.SolidWaste)]
    public static partial DrawerType ToDrawerType(this EnumSampleDrawer source);

    [MapEnumValue(EnumSampleArea.UnspecifiedSampleArea, SampleArea.None)]
    public static partial SampleArea ToSampleArea(this EnumSampleArea source);

    [MapEnumValue(SampleTubeIdentitySource.ConfirmManualInput, EnumIdentitySource.ManualInput)]
    [MapperIgnoreTargetValue(EnumIdentitySource.UnspecifiedIdentitySource)]
    public static partial EnumIdentitySource ToEnumIdentitySource(this SampleTubeIdentitySource source);

    [MapEnumValue(SampleType.None, EnumSampleType.UnspecifiedSampleType)]
    public static partial EnumSampleType ToEnumSampleType(this SampleType source);

    [MapperIgnoreSource(nameof(SampleTube.Id))]
    [MapperIgnoreSource(nameof(SampleTube.OriginalPosition))]
    [MapperIgnoreSource(nameof(SampleTube.Area))]
    [MapperIgnoreSource(nameof(SampleTube.DomainEvents))]
    public static partial void UpdateSampleTubeFromStorage(this SampleTube source, SampleTube target);

    [MapProperty(nameof(InstrumentSampleTube.Error), nameof(SampleTube.InstrumentError))]
    [MapProperty(nameof(@InstrumentSampleTube.Position.Area), nameof(SampleTube.Area))]
    [MapProperty(nameof(@InstrumentSampleTube.Position.TrayBarcode), nameof(SampleTube.TrayId))]
    [MapProperty(nameof(@InstrumentSampleTube.Position.TrayPosition), nameof(SampleTube.TrayPosition))]
    [MapProperty(nameof(@InstrumentSampleTube.Position.TubePosition), nameof(SampleTube.TubePosition))]
    [MapProperty(nameof(@InstrumentSampleTube.OriginalPosition.TrayBarcode), nameof(@SampleTube.OriginalPosition.TrayId))]
    [MapperIgnoreSource(nameof(InstrumentSampleTube.SampleQcStatus))]
    [MapperIgnoreSource(nameof(InstrumentSampleTube.ChangeType))]
    private static partial void MapInstrumentToSampleTube(this InstrumentSampleTube source, SampleTube target);

    [MapProperty(nameof(InstrumentSampleTray.TrayBarcode), nameof(SampleTray.Id))]
    private static partial SampleTray ToSampleTray(this InstrumentSampleTray? source);

    [MapEnumValue(EnumSampleTrayError.SampleTrayErrorUnknownObject, SampleTrayError.UnknownObject)]
    [MapEnumValue(EnumSampleTrayError.SampleTrayErrorUnreadableBarcode, SampleTrayError.UnreadableBarcode)]
    [MapEnumValue(EnumSampleTrayError.SampleTrayErrorInvalidBarcode, SampleTrayError.InvalidBarcode)]
    [MapEnumValue(EnumSampleTrayError.SampleTrayErrorDuplicateBarcode, SampleTrayError.DuplicateBarcode)]
    [MapEnumValue(EnumSampleTrayError.UnspecifiedSampleTrayError, SampleTrayError.None)]
    private static partial SampleTrayError ToSampleTrayError(this EnumSampleTrayError source);

    [MapEnumValue(EnumSampleType.UnspecifiedSampleType, SampleType.None)]
    private static partial SampleType ToSampleType(this EnumSampleType source);

    [MapEnumValue(EnumSampleQcTubeStatus.SampleQcTubeStatusCompleted, SampleTubeStatus.PipettedQc)]
    [MapEnumValue(EnumSampleQcTubeStatus.SampleQcTubeStatusInProgress, SampleTubeStatus.InProgress)]
    [MapEnumValue(EnumSampleQcTubeStatus.SampleQcTubeStatusEmpty, SampleTubeStatus.Completed)]
    [MapEnumValue(EnumSampleQcTubeStatus.SampleQcTubeStatusNotNeeded, SampleTubeStatus.None)]
    [MapEnumValue(EnumSampleQcTubeStatus.UnspecifiedSampleQcTubeStatus, SampleTubeStatus.None)]
    [MapperIgnoreTargetValue(SampleTubeStatus.NotIdentified)]
    [MapperIgnoreTargetValue(SampleTubeStatus.FollowUpReflex)]
    [MapperIgnoreTargetValue(SampleTubeStatus.PartlyCompleted)]
    private static partial SampleTubeStatus ToSampleTubeStatus(this EnumSampleQcTubeStatus source);

    [MapEnumValue(EnumSampleTubeError.UnspecifiedSampleTubeError, SampleTubeError.None)]
    [MapperIgnoreTargetValue(SampleTubeError.QcNotNeeded)]
    [MapperIgnoreTargetValue(SampleTubeError.QcOutOfRange)]
    [MapperIgnoreTargetValue(SampleTubeError.MethodsNotActive)]
    [MapperIgnoreTargetValue(SampleTubeError.MissingTestOrders)]
    private static partial SampleTubeError ToSampleTubeError(this EnumSampleTubeError source);

    [MapEnumValue(EnumIdentitySource.UnspecifiedIdentitySource, SampleTubeIdentitySource.BarcodeScanning)]
    [MapperIgnoreTargetValue(SampleTubeIdentitySource.ConfirmManualInput)]
    private static partial SampleTubeIdentitySource ToSampleTubeIdentitySource(this EnumIdentitySource source);

    private static DateTimeOffset? ToDateTimeOffsetNullable(this Timestamp? source)
    {
        return source?.ToDateTimeOffset().ToLocalTime();
    }
}
