using MediatR;
using Pharos.Server.Domain.Entities;

namespace Pharos.Server.Application.Sample;

/// <summary>
/// Gets current sample tubes for one request by combining live in-memory tubes and relevant persisted tubes.
/// </summary>
/// <param name="RequestUids">The unique request identifiers used to scope tubes to requests.</param>
public record GetCurrentSampleTubesQuery(IReadOnlyCollection<RequestUid> RequestUids)
    : IRequest<List<CurrentSampleTubesDto>>;

/// <summary>
/// Gets current sample tubes for request-list display, where Last Tray follows the current Tube tab rows.
/// </summary>
/// <param name="RequestUids">The unique request identifiers used to scope request-list rows.</param>
public record GetRequestListSampleTubesQuery(IReadOnlyCollection<RequestUid> RequestUids)
    : IRequest<List<CurrentSampleTubesDto>>;

public record CurrentSampleTubesDto(
    RequestUid RequestUid,
    List<CurrentSampleTubeDto> Tubes);

public record CurrentSampleTubeDto(
    string? InstrumentNumber,
    string? TrayId,
    int? TubePosition,
    DateTimeOffset? LoadTimestamp,
    string? TubePositionDisplayValue = null);
