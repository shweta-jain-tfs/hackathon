namespace Pharos.Contracts.Notifications;

public interface ISampleTrayHub : IEntityHub<string>
{
    Task PositionStatusUpdated(List<string> trayIds);

    Task FreePositionAndErrorCountUpdated();
}