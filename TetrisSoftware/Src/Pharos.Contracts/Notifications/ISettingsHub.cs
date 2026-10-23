namespace Pharos.Contracts.Notifications;

public interface ISettingsHub : IHub
{
    Task SettingsChanged(List<string> updatedKeys, List<string> deletedKeys, string? changedBy);
}
