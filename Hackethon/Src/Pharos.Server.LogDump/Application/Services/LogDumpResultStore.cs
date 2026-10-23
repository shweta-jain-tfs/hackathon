using Pharos.Server.LogDump.Application.Interfaces;

namespace Pharos.Server.LogDump.Application.Services;

internal sealed class LogDumpResultStore : ILogDumpResultStore
{
    public LogDumpResult? Result { get; set; }
}