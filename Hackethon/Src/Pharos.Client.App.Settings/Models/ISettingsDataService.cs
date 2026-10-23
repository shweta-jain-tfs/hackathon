namespace Pharos.Client.App.Settings.Models;

public interface ISettingsDataService<TModel> : IEntityDataService<TModel>
    where TModel : IEntityModel<int>
{
    public IEnumerable<Page> KeyItemsPages { get; }
    Task<TModel> GetPageAsync(string rootPageLabel, string key);
    Task DeleteSettingsAsync(string[] keys);
    Task<DeleteResponse> DeletePagesAsync(string[] pageListEntryKeys);
    Task<Dictionary<string, string>> GetMenuItemsAsync();
    Task<Dictionary<string, string>> RefreshPageRouteMapAsync(CancellationToken cancellationToken = default);
    Task<string> ExportSettingsAsync();
    Task<SettingsImportResult> ImportSettingsAsync(ImportSettingsCommand importSettingsCommand);
    Task<CopySettingsResult> CopySettingsAsync(CopySettingsCommand copySettingsCommand);
    Task<SettingsUpdateResult> UpdateSettingAsync(TModel value, bool isPageListUpdate);
    Task<CheckPageListMaxItemsResult> CheckPageListMaxItemsAsync(string pageListKey, string rootPageLabel);
}