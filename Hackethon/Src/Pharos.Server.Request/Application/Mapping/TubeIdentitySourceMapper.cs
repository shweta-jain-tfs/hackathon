using Riok.Mapperly.Abstractions;
using Tetris.Protocol.Common;

namespace Pharos.Server.Request.Application.Mapping;

[Mapper]
internal static partial class TubeIdentitySourceMapper
{
    [MapEnumValue(SampleTubeIdentitySource.ConfirmManualInput, EnumIdentitySource.ManualInput)]
    [MapperIgnoreTargetValue(EnumIdentitySource.UnspecifiedIdentitySource)]
    internal static partial EnumIdentitySource ToIdentitySource(this SampleTubeIdentitySource source);
}