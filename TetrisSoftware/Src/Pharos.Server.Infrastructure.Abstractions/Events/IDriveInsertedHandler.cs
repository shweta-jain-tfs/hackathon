namespace Pharos.Server.Infrastructure.Events;
public interface IDriveInsertedHandler
{
    Task OnDriveInsertedAsync(string driveName, CancellationToken cancellationToken);
}
