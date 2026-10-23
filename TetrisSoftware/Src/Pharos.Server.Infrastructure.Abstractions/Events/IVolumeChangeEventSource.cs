namespace Pharos.Server.Infrastructure.Events;
public interface IVolumeChangeEventSource : IDisposable
{
    event Action<ushort, string> VolumeChanged;
    void Start(CancellationToken stoppingToken);
}
