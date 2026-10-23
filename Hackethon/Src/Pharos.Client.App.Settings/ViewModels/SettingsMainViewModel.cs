using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.Settings.ViewModels.Converters;
using Pharos.Client.Common.App.PageRouting;
using Pharos.Client.Common.Localization;
using Pharos.Common.Web.Components.Modal;
using Pharos.Common.Web.Components.NavigationLock;

namespace Pharos.Client.App.Settings.ViewModels;

public sealed class SettingsMainViewModel(ISettingsDataService<SettingItemBase> dataService, IConverter converter, IStringLocalizer<Translations> loc) : EntityListViewModel<SettingItemBase>(dataService)
{
    public NavigationManager NavigationManager { get; set; } = default!;
    public Func<Task>? ShowSaveSettingsErrorDialog { get; set; }
    public IModalService Modal { get; set; } = default!;

    /// <summary>
    ///  Selected View, currently used when preventing the user to navigate
    /// when there are unsaved changes.
    /// </summary>
    public PageViewModel? Selected { get; set; }

    /// <summary>
    /// Used to share the save settings functionality with other components.
    /// </summary>
    public EventCallback Save { get; set; }

    /// <summary>
    /// Used to share the discard settings functionality with other components.
    /// </summary>
    public EventCallback DiscardChanges { get; set; }

    private SettingsNavigationService SettingsNavigationService { get; set; } = new();

    public async Task<SettingsViewModelBase> GetPageAsync(string url)
    {
        var pageKey = await GetPageKeyAsync(url);
        if (pageKey.IsLocal)
        {
            // Creating a new entry in pagelist
            return pageKey.LocalPage!;
        }

        var segments = pageKey.Key!.Split(":").ToList();
        var rootPage = segments.First();
        try
        {
            var page = (Page)await dataService.GetPageAsync(rootPage, pageKey.Key);
            return converter.Convert(page);
        }
        catch (Exception ex) when (MissingSettingsPageExceptionMatcher.IsMatch(ex, pageKey.Key))
        {
            var refreshedPageKey = await RefreshPageKeyAsync(url);

            if (refreshedPageKey.IsLocal)
            {
                return refreshedPageKey.LocalPage!;
            }

            var refreshedKey = refreshedPageKey.Key!;
            if (string.Equals(refreshedKey, pageKey.Key, StringComparison.Ordinal))
            {
                throw;
            }

            var refreshedSegments = refreshedKey.Split(":").ToList();
            var refreshedRootPage = refreshedSegments.First();
            var refreshedPage = (Page)await dataService.GetPageAsync(refreshedRootPage, refreshedKey);
            return converter.Convert(refreshedPage);
        }
    }

    private async Task<SettingsNavigationService.PageKey> GetPageKeyAsync(string url)
    {
        if (SettingsNavigationService.TryGetPageKey(url, out var pageKey))
        {
            return pageKey;
        }

        return await RefreshPageKeyAsync(url);
    }

    private async Task<SettingsNavigationService.PageKey> RefreshPageKeyAsync(string url)
    {
        var pageRouteMap = await dataService.RefreshPageRouteMapAsync();
        SettingsNavigationService.SetPageRouteMap(pageRouteMap);
        return SettingsNavigationService.GetPageKey(url);
    }

    public string GetPathFromKey(string key, string? preferredPath = null)
    {
        return SettingsNavigationService.GetPathFromKey(key, preferredPath);
    }

    public bool TryGetPathFromKey(string key, string? preferredPath, [NotNullWhen(true)] out string? path)
    {
        if (SettingsNavigationService.TryGetPathFromKey(key, preferredPath, out var resolvedPath))
        {
            path = resolvedPath;
            return true;
        }

        path = null;
        return false;
    }

    public bool IsLocalPage(string path)
    {
        return SettingsNavigationService.GetPageKey(path).IsLocal;
    }

    public async Task<Dictionary<string, string>> GetMenuItemsAsync()
    {
        return await dataService.GetMenuItemsAsync();
    }

    public Task<Dictionary<string, string>> RefreshPageRouteMap(
        CancellationToken cancellationToken = default)
    {
        return dataService.RefreshPageRouteMapAsync(
            cancellationToken);
    }

    public async Task DeleteTableEntryItems(List<string> entryIds)
    {
        await dataService.DeleteSettingsAsync(entryIds.ToArray());
    }

    public async Task<DeleteResponse?> DeletePageListEntry(PageListViewModel viewModel)
    {
        var selectedEntryKeys = viewModel.Values.Where(e => e.IsSelected)
            .Select(e => e.EntryKey!).ToList();
        if (selectedEntryKeys.Count == 0)
        {
            return null;
        }

        // Delete selected page entries
        return await dataService.DeletePagesAsync(selectedEntryKeys.ToArray());
    }

    public async Task<SettingsUpdateResult> Update(SettingItemBase page, bool isPageListUpdate)
    {
        return await dataService.UpdateSettingAsync(page, isPageListUpdate);
    }

    public void SetPageRouteMap(Dictionary<string, string> pageRouteMap)
    {
        SettingsNavigationService.SetPageRouteMap(pageRouteMap);
    }

    public void AddLocalKeyPage([NotNull] SettingsViewModelBase viewModel, string path)
    {
        SettingsNavigationService.AddPageKey(path, viewModel.Model!.Key, true, viewModel);
    }

    public async Task<string> ExportSettings()
    {
        return await dataService.ExportSettingsAsync();
    }

    public async Task<SettingsImportResult> ImportSettings(ImportSettingsCommand importSettingsCommand)
    {
        return await dataService.ImportSettingsAsync(importSettingsCommand);
    }

    public async Task<CheckPageListMaxItemsResult> CheckPageListMaxItemsAsync(string pageListKey, string rootPageLabel)
    {
        return await dataService.CheckPageListMaxItemsAsync(pageListKey, rootPageLabel);
    }

    public async Task<(bool CanAdd, string? ErrorMessage)> ValidateCanAddNewPageListEntryAsync(PageListViewModel viewModel)
    {
        var pageListModel = viewModel.Model as PageList;
        if (pageListModel?.Key == null)
        {
            return (true, null);
        }

        var rootPageLabel = pageListModel.Key.Split(':').First();
        var checkResult = await CheckPageListMaxItemsAsync(pageListModel.Key, rootPageLabel);

        if (!checkResult.CanAdd)
        {
            var errorMessage = checkResult.MaxItems.HasValue
                ? string.Format(loc["Validation_PageListMaxItemsReached"], checkResult.MaxItems.Value)
                : loc["Validation_PageListMaxItemsReached"].Value.Replace(" ({0})", "");
            return (false, errorMessage);
        }

        return (true, null);
    }

    public async Task GoToSubSettings(string path, string uri)
    {
        if (IsLocalPage(path))
        {
            if (Selected!.IsModified)
            {
                await ShowUnsavedChangesDialog();
            }
            else
            {
                await ShowSaveSettingsErrorDialog!.Invoke();
            }
        }
        else
        {
            var subSettingsUri = $"{PageRoute.Settings}/{uri}";
            NavigationManager.NavigateTo(subSettingsUri);
        }
    }

    public async Task<bool> ShowUnsavedChangesDialog()
    {
        var modalTitle = loc["UnsavedChangesDialogTitle"];
        var modal = Modal.Show<PhNavigationConfirmation>(new ModalOptions() { Title = modalTitle });
        var ret = await modal.Result;

        switch (ret.Data)
        {
            // Cancel (X button)
            case null:
                return true;
            // Save
            case true when Selected?.IsValid == true:
                await Save.InvokeAsync();
                return false;
            // Invalid Save
            case true:
                await Save.InvokeAsync();
                return true;
            // Discard
            default:
                await DiscardChanges.InvokeAsync();
                return false;
        }
    }
}
