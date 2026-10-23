using System.Diagnostics;
using Pharos.Server.Application;

namespace Pharos.Server.Services;

internal class PharosVersionService : IPharosVersionService
{
    private DateTimeOffset? _startedAt;

    public string Version
    {
        get
        {
            field ??= GetPharosVersion();
            return field;
        }
    }

    public DateTimeOffset StartedAt
    {
        get
        {
            _startedAt ??= new DateTimeOffset(Process.GetCurrentProcess().StartTime.ToUniversalTime(), TimeSpan.Zero);
            return _startedAt.Value;
        }
    }

    private string GetPharosVersion()
    {
        return typeof(Program).Assembly.GetName().Version?.ToString() ?? throw new InvalidOperationException($"Failed to get version from {nameof(Program)} assembly");
    }
}
