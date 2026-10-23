using LabCommunity.API.Client;
using Pharos.Server.Application.LabCommunity;
using Riok.Mapperly.Abstractions;

namespace Pharos.Server.LabCommunity.Application.Mapping;

[Mapper]
internal static partial class LabCommunityMapper
{
    [MapEnumValue(Pharos.Server.Domain.Enums.InstrumentType.Small, InstrumentType.E500)]
    [MapEnumValue(Pharos.Server.Domain.Enums.InstrumentType.Medium, InstrumentType.E2000)]
    public static partial InstrumentType ToLabCommunityInstrumentType(this Pharos.Server.Domain.Enums.InstrumentType instrumentType);

    public static partial CurveControlJudgment ToLabCommunityCurveControlJudgment(this CurveControlResultJudgment judgment);

    public static partial QualityControlJudgment ToLabCommunityQualityControlJudgment(this QualityControlResultJudgment judgment);

    [MapperIgnoreTargetValue(BlankType.ReagentBlank)]
    public static partial BlankType ToLabCommunityBlankType(this BlankResultType blankType);

    public static AssayTechnology ToLabCommunityAssayTechnology(this BlankResultType blankType)
    {
        // Rinse blanks are measured as ImmunoCAP results; see ISW Feature Description - Priming and
        // Blanking (881265), section describing Rinse Blank.
        return blankType == BlankResultType.ReagentBlankEliA ? AssayTechnology.EliA : AssayTechnology.ImmunoCap;
    }

    public static partial InstrumentErrorClassification ToLabCommunityErrorClassification(
        this Pharos.Server.Domain.Enums.ErrorClassification classification);
}