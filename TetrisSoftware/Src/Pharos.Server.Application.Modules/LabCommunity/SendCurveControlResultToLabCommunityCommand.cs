using MediatR;

namespace Pharos.Server.Application.LabCommunity;

public sealed record SendCurveControlResultToLabCommunityCommand(
    Guid GroupId,
    string InstrumentNumber,
    DateTimeOffset MeasurementTime,
    string CurveControlName,
    string MethodName,
    string TestName,
    CurveControlResultJudgment Judgment,
    double ExpectedConcentration,
    string? ConjugateLotNumber,
    string? SolidPhaseLotNumber,
    string? CurveControlLotNumber,
    IReadOnlyList<CurveControlResultReplicate> Replicates) : IRequest;

public sealed record CurveControlResultReplicate(
    int ReplicateNumber,
    DateTimeOffset MeasurementTime,
    double? Concentration,
    double? Response,
    CurveControlResultJudgment Judgment,
    string? ConjugateLotNumber,
    string? SolidPhaseLotNumber,
    string? CurveControlLotNumber);

public enum CurveControlResultJudgment
{
    NotDefined = 0,
    Ok = 1,
    FlaggedHigh = 2,
    FlaggedLow = 3,
    High = 4,
    Low = 5
}