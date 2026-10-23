using MediatR;

namespace Pharos.Server.Application.LabCommunity;

public sealed record SendBlankResultToLabCommunityCommand(
    int BlankId,
    string InstrumentNumber,
    DateTimeOffset MeasurementTime,
    BlankResultType BlankType,
    bool ResultOk,
    string? DevelopmentSolutionBarcode,
    string? StopSolutionBarcode,
    IReadOnlyList<BlankResultReplicate> Replicates) : IRequest;

public sealed record BlankResultReplicate(
    int ReplicateNumber,
    DateTimeOffset MeasurementTime,
    double Voltage);

public enum BlankResultType
{
    Unspecified = 0,
    ReagentBlankImmunoCap = 1,
    ReagentBlankEliA = 2,
    RinseBlank = 3,
    DarkBlank = 4,
    WashBlank = 5
}
