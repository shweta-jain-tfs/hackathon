namespace Pharos.Server.Infrastructure.Events;
public interface IDriveRemovedHandler
{
    Task OnDriveRemovedAsync(string driveName, CancellationToken cancellationToken);
}
