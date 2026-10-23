using Pharos.Server.Application.Instrument;
using Pharos.Server.Instrument.Application.Commands.SendCmrTestOrder;
using Pharos.Server.Instrument.Application.Queries.GetInstruments;
using Riok.Mapperly.Abstractions;
using Tetris.Protocol.Instrument.TestOrder;

namespace Pharos.Server.Instrument.Application.Mapping;

[Mapper]
internal static partial class InstrumentMapper
{
    [MapProperty(nameof(Domain.Instrument.ProcessStatus.RemainingResults), nameof(InstrumentDto.NumberOfTests))]
    [MapperIgnoreTarget(nameof(InstrumentDto.AssaySchedulerIsActive))]
    [MapperIgnoreTarget(nameof(InstrumentDto.AssaySchedulerIsPausedUntilDate))]
    [MapperIgnoreTarget(nameof(InstrumentDto.AssaySchedulerNextRun))]
    [MapperIgnoreTarget(nameof(InstrumentDto.AssaySchedulerNextRunType))]
    [MapperIgnoreTarget(nameof(InstrumentDto.AssaySchedulerShowInfo))]
    [MapperIgnoreTarget(nameof(InstrumentDto.SelectedTechnologies))]
    public static partial InstrumentDto ToInstrumentDto(this Domain.Instrument source);
    public static partial CmrTestOrderResultDto ToCmrTestOrderResultDto(this CmrTestOrderResponse source);

    public static partial ModuleType ToModuleType(this Tetris.Protocol.Pharos.EnumModuleType source);

    [MapEnumValue(EnumCmrTestOrderStatus.UnspecifiedCmrTestOrderStatus, CmrTestOrderStatus.Unspecified)]
    [MapperIgnoreTargetValue(CmrTestOrderStatus.NotAllowed)]
    private static partial CmrTestOrderStatus MapEnumCmrTestOrderStatus(EnumCmrTestOrderStatus source);
}
