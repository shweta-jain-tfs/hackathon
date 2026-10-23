using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Result.Application.Queries.GetCalibrationCurve;
using Pharos.Server.Application.LabCommunity;
using Pharos.Server.Result.Domain;
using Riok.Mapperly.Abstractions;

namespace Pharos.Server.Result.Application.Mapping;

[Mapper]
internal static partial class ResultMapper
{
    public static partial CurveControlResultJudgment ToCurveControlResultJudgment(this CurveControlJudgment source);

    public static partial QualityControlResultJudgment ToQualityControlResultJudgment(this QualityControlJudgement source);

    public static partial BlankResultType ToBlankResultType(this BlankType source);

    [MapProperty(nameof(CalibratorTestResult.OperatorComment), nameof(CalibratorTestResultDto.Comment))]
    [MapperIgnoreTarget(nameof(CalibratorTestResultDto.Unit))]
    [MapperIgnoreTarget(nameof(CalibratorTestResultDto.Technology))]
    public static partial CalibratorTestResultDto ToCalibratorTestResultDto(CalibratorTestResult source);

    public static CalibratorTestReplicateDto ToCalibratorTestReplicateDto(this TestReplicate source)
    {
        var calibratorReplicate = (CalibratorTestReplicate)source;
        var dto = calibratorReplicate.MapCalibratorTestReplicateDto();
        dto.HasError = calibratorReplicate.TestReplicateErrors is { Count: > 0 };
        return dto;
    }

    [MapperIgnoreTarget(nameof(CalibratorTestReplicateDto.HasError))]
    private static partial CalibratorTestReplicateDto MapCalibratorTestReplicateDto(this CalibratorTestReplicate source);
    
    public static partial MethodPresentationDto? ToMethodPresentationDto(MethodPresentation? source);
}