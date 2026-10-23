using MediatR;

namespace Pharos.Server.Application.LogDump;

/// <summary>
/// Notifies that a log dump file was successfully created and stored in temporary file storage.
/// </summary>
/// <param name="FileId">The unique ID of the log dump file.</param>
/// <param name="Source">The source of the log dump request.</param>
public sealed record LogDumpCreatedEvent(Guid FileId, LogDumpRequestSource Source) : INotification;