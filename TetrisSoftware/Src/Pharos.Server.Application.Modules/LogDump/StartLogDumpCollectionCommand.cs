using MediatR;

namespace Pharos.Server.Application.LogDump;

/// <summary>
/// Schedules background log dump collection for the specified source (Pharos or Lab Community).
/// Returns true if the log dump collection was successfully scheduled, false otherwise.
/// </summary>
public sealed record StartLogDumpCollectionCommand(LogDumpCollectionRequest Request, LogDumpRequestSource Source) : IRequest<bool>;

public sealed record LogDumpCollectionRequest(
    LogCollectionDateMode DateMode,
    DateOnly? SingleDate,
    DateOnly? FromDate,
    DateOnly? ToDate,
    LogFileSelection PharosFiles,
    LogFileSelection InstrumentFiles);
