using Pharos.Server.Application.LogDump;

namespace Pharos.Server.LogDump.Application.Interfaces;

public interface ILogDumpResultStore
{
    LogDumpResult? Result { get; }
}

public sealed record LogDumpResult(Guid FileId, string FileName, DateTimeOffset FileCreatedDateTime, LogDumpRequestSource Source);