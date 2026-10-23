namespace Pharos.Contracts.Notifications;

public interface ICurrentUserHub : IHub
{
    Task UserInfoChanged();
    Task AutoLogoutSettingsChanged();
}
