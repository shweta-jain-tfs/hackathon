using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Flr.Application.Parsers;
using Riok.Mapperly.Abstractions;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos.TestResult;

namespace Pharos.Server.Result.Flr.Application.Mapping;

[Mapper]
internal static partial class FlrMapper
{
    [MapEnumValue(ArticleType.None, EnumReagentType.UnspecifiedReagentType)]
    [MapEnumValue(ArticleType.EliaCalibratorWellCarrier, EnumReagentType.EliaCalibrationWellCarrier)]
    [MapEnumValue(ArticleType.FluoroCVial, EnumReagentType.FluoroC)]
    [MapEnumValue(ArticleType.MaintenanceConcentrateBottle, EnumReagentType.MaintenanceConcentrate)]
    [MapEnumValue(ArticleType.StopSolutionBottle, EnumReagentType.StopSolution)]
    [MapEnumValue(ArticleType.WashAdditiveBottle, EnumReagentType.WashAdditive)]
    [MapEnumValue(ArticleType.WashConcentrateBottle, EnumReagentType.WashConcentrate)]
    [MapperIgnoreSourceValue(ArticleType.DilutionPlate)]
    [MapperIgnoreTargetValue(EnumReagentType.DilutionPlate)]
    public static partial EnumReagentType ToEnumReagentType(this ArticleType source);

    [MapEnumValue(FlrSampleType.Unspecified, EnumTestType.UnspecifiedTestType)]
    public static partial EnumTestType ToEnumTestType(this FlrSampleType source);
}
