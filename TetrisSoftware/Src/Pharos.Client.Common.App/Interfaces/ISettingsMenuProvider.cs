namespace Pharos.Client.Common.App.Interfaces;

public interface ISettingsMenuProvider
{
    Task<IReadOnlyList<RouteSubMenuItem>> GetSettingsMenuItemsAsync(CancellationToken cancellationToken = default);
}