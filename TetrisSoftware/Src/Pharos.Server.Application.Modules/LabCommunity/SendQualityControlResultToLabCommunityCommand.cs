using MediatR;

namespace Pharos.Server.Application.LabCommunity;

public sealed record SendQualityControlResultToLabCommunityCommand(
    Guid GroupId,
    string InstrumentNumber,
    DateTimeOffset MeasurementTime,
    string QualityControlName,
    string MethodName,
    string TestName,
    QualityControlResultJudgment Judgment,
    string? ConjugateLotNumber,
    string? SolidPhaseLotNumber,
    string? QualityControlLotNumber,
    double RangeMin,
    double RangeMax,
    double PercentCvLimit,
    IReadOnlyList<QualityControlResultReplicate> Replicates) : IRequest;

public sealed record QualityControlResultReplicate(
    int ReplicateNumber,
    DateTimeOffset MeasurementTime,
    double? Concentration,
    double? Response,
    QualityControlResultJudgment Judgment,
    string? ConjugateLotNumber,
    string? SolidPhaseLotNumber,
    string? QualityControlLotNumber);

public enum QualityControlResultJudgment
{
    NotDefined = 0,
    Ok = 1,
    FlaggedHigh = 2,
    FlaggedLow = 3,
    High = 4,
    Low = 5,
    HighCv = 6
}
