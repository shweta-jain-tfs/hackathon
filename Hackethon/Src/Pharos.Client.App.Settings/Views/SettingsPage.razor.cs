using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Pharos.Client.App.Settings.Actions;
using Pharos.Client.App.Settings.Views.Settings;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.PageRouting;
using Pharos.Client.Common.App.Services;
using Pharos.Client.Common.Localization;
using Pharos.Contracts.Settings;
using Pharos.Client.Common.ViewModels.Interfaces;
using Pharos.Common.Web.Components.Breadcrumb;
using Pharos.Common.Web.Components.DataGrid;
using Pharos.Common.Web.Components.FormElements.Enums;
using Pharos.Common.Web.Components.Icons;
using Pharos.Common.Web.Components.Modal;
using Pharos.Common.Web.Components.Toaster;
using Pharos.Common.Web.Extensions;
using Pharos.Common.Web.Utilities;

namespace Pharos.Client.App.Settings.Views;

public partial class SettingsPage
{
    private const string _tubeTypesPageListKey = SettingsKeys.Samples.TubeTypes.TubeTypeList;
    private const string _settingsKeyDelimiter = ":";

    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    [Inject] private SettingsMainViewModel ViewModelList { get; set; } = default!;

    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    [Inject] public IModalService Modal { get; set; } = default!;

    [Inject] private IToasterService ToasterService { get; set; } = default!;

    [Inject] private IEnumerable<ISettingsAction> SettingsActions { get; set; } = default!;

    [Inject] private UiSettingsService UiSettingsService { get; set; } = default!;

    [Inject] private IAuthorizationService AuthorizationService { get; set; } = default!;

    [Inject] private ISettingsNotificationProvider SettingsNotificationProvider { get; set; } = default!;

    [Inject] private ILogger<SettingsPage> Logger { get; set; } = default!;

    [Inject] private IExceptionHandlerService ExceptionHandlerService { get; set; } = default!;

    [Parameter] public string Path { get; set; } = null!;

    private PhDataGrid<TableRowViewModel> _phDataGrid = default!;
    private bool _isNavigating;
    private bool _isSaving;
    private bool _isHandlingExternalRefresh;
    private bool _navigationDeferredDuringSave;
    private string? _lastLoadedPath;
    private CancellationTokenSource? _loadCts;

    private bool _disposed;

    private Dictionary<ActionKey, ISettingsAction> _settingsActions = [];

    private bool _isRndMode;

    private CancellationTokenSource? _externalRefreshCts;
    private bool _externalSettingsChangeModalOpen;
    private SettingsChangedEventArgs? _pendingExternalDeletionChange;

    public static bool IsMaxNumberOfItemsReached(PageListViewModel viewModel)
    {
        var maxItems = (viewModel.Model as PageList)?.MaxItems;
        return maxItems is not null && viewModel.Values.Count >= maxItems.Value;
    }

    public PageViewModel? Selected
    {
        get;
        set
        {
            field = value!;
            ViewModelList.Selected = value;

            // this defines the Save and DiscardChanges event callbacks, to be used when there are unsaved changes
            // not intercepted by the navigation lock
            ViewModelList.Save = EventCallback.Factory.Create(this, Save);
            ViewModelList.DiscardChanges = EventCallback.Factory.Create(this, DiscardChanges);
        }
    }

    protected List<BreadcrumbItem> Breadcrumbs { get; set; } = [];

    private bool ShowBreadcrumbs { get; set; }

    private string SettingsPageCssClass => new CssBuilder("ph-settings-page")
        .AddClass("ph-settings-page--saving", _isSaving)
        .Build();

    private List<BreadcrumbItem> Items { get; set; } = [];

    private string? GroupToScroll { get; set; }

    private string? _targetUrl;
    private string? _deletedPageNavigationPath;

    private IDisposable? _registration;

    private IJSObjectReference? _script;
    private readonly Dictionary<SettingsActionScript, IJSObjectReference> _settingsActionScripts = [];

    protected override async Task OnInitializedAsync()
    {
        ViewModelList.ShowSaveSettingsErrorDialog = async () => await ShowSaveSettingsErrorDialog();
        ViewModelList.NavigationManager = NavigationManager;
        ViewModelList.Modal = Modal;
        _settingsActions = SettingsActions.ToDictionary(action => action.ActionKey);
        _isRndMode = await AuthorizationService.IsRndMode();
        UiSettingsService.SettingsChanged += OnSettingsChangedAsync;
        SettingsNotificationProvider.SettingsChanged += OnExternalSettingsChanged;

        await base.OnInitializedAsync();
    }

    private async Task OnSettingsChangedAsync()
    {
        _isRndMode = await AuthorizationService.IsRndMode();
        await InvokeAsync(StateHasChanged);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _registration = NavigationManager.RegisterLocationChangingHandler(LocationChangingHandler);
            _script = await ImportCommonUtilityJSAsync("scroll.js");
            await LoadSettingsActionScripts();
        }
        if (_script != null && GroupToScroll != null)
        {
            await _script.InvokeVoidAsync("scrollToElement", GroupToScroll);
            GroupToScroll = null;
        }

        if (!string.IsNullOrEmpty(_deletedPageNavigationPath))
        {
            var deletedPageNavigationPath = _deletedPageNavigationPath;
            _deletedPageNavigationPath = null;
            NavigateToSettingsRootPath(deletedPageNavigationPath);
        }
    }

    private ValueTask LocationChangingHandler(LocationChangingContext arg)
    {
        if (!_isSaving)
        {
            return ValueTask.CompletedTask;
        }

        // Prevent navigation while saving, but remember that we need to navigate after save completes.
        _targetUrl = arg.TargetLocation;
        _navigationDeferredDuringSave = true;
        arg.PreventNavigation();

        return ValueTask.CompletedTask;
    }

    protected override async Task OnParametersSetAsync()
    {
        if (_disposed)
        {
            return;
        }

        if (string.Equals(_lastLoadedPath, Path, StringComparison.Ordinal))
        {
            return;
        }

        // Cancel any page load still in flight from a previous navigation so its
        // result or redirect cannot override the page the user just navigated to.
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = new CancellationTokenSource();
        var cancellationToken = _loadCts.Token;

        _lastLoadedPath = Path;

        var selected = await RetrievePageFromPath(Path, cancellationToken);

        // A newer navigation started while awaiting the page data; discard this stale result
        // so the previously requested (e.g. General) page is not shown instead.
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        Selected = selected;

        ShowBreadcrumbs = true;
        await InitializeBreadcrumbs();
    }

    public async Task CreateNewPageListEntry(PageListViewModel viewModel)
    {
        // Prevent spam clicks during navigation
        if (_isNavigating)
        {
            return;
        }

        // Client-side check
        if (IsMaxNumberOfItemsReached(viewModel))
        {
            return;
        }

        var index = viewModel.Values.FindIndex(row => row.IsSelected);
        viewModel.OriginPage = NavigationManager.Uri;

        if (Selected?.IsModified == true)
        {
            var userCancelledOrSaveFailed = await ViewModelList.ShowUnsavedChangesDialog();
            if (userCancelledOrSaveFailed)
            {
                return;
            }
        }

        _isNavigating = true;
        try
        {
            // Server-side validation before creating the entry
            var (canAdd, errorMessage) = await ViewModelList.ValidateCanAddNewPageListEntryAsync(viewModel);
            if (!canAdd)
            {
                ToasterService.ShowToast(ToastType.Warning, errorMessage!);
                return;
            }

            await ReloadPageRouteMap();
            var pageViewModel = await viewModel.AddNewRow();
            pageViewModel.OriginPage = viewModel.OriginPage;
            var path = pageViewModel.Url;
            ViewModelList.AddLocalKeyPage(pageViewModel, path);

            NavigationManager.NavigateTo($"{PageRoute.Settings}/{EncodeUrlPath(path)}");
        }
        finally
        {
            _isNavigating = false;
        }
    }

    public async Task CopyPageListViewModel(PageListViewModel viewModel)
    {
        // Prevent spam clicks during navigation
        if (_isNavigating)
        {
            return;
        }

        if (IsMaxNumberOfItemsReached(viewModel))
        {
            return;
        }

        viewModel.OriginPage = NavigationManager.Uri;

        _isNavigating = true;
        try
        {
            await ReloadPageRouteMap();
            var result = await viewModel.CopySelectedRow();

            if (!result.Success)
            {
                var errorMessage = GetCopySettingsErrorMessage(result.ErrorCode, result.MaxItemsLimit);
                var toastType = result.IsWarning ? ToastType.Warning : ToastType.Alarm;
                ToasterService.ShowToast(toastType, errorMessage);
                return;
            }

            await _phDataGrid.ClearSelectedItem();
            await ReloadPage();
        }
        finally
        {
            _isNavigating = false;
        }
    }

    private string GetCopySettingsErrorMessage(CopySettingsErrorCode? errorCode, int? maxItemsLimit)
    {
        return errorCode switch
        {
            CopySettingsErrorCode.ParentPageListNotFound => Loc["CopySettings_Error_ParentPageListNotFound"],
            CopySettingsErrorCode.MaxItemsReached when maxItemsLimit.HasValue =>
                string.Format(Loc["CopySettings_Error_MaxItemsReached"], maxItemsLimit.Value),
            CopySettingsErrorCode.MaxItemsReached =>
                Loc["CopySettings_Error_MaxItemsReached"].Value.Replace(" ({0})", ""),
            CopySettingsErrorCode.CopyDisabled => Loc["CopySettings_Error_CopyDisabled"],
            CopySettingsErrorCode.SourcePageNotFound => Loc["CopySettings_Error_SourcePageNotFound"],
            CopySettingsErrorCode.NameTooLong => Loc["CopySettings_Error_NameTooLong"],
            CopySettingsErrorCode.UnknownError => Loc["CopySettings_Error_UnknownError"],
            _ => Loc["SettingsPage_Copy_Failed"]
        };
    }

    private Task<PageViewModel> RetrievePageFromPath(
        string url,
        CancellationToken cancellationToken = default)
    {
        return RetrievePageFromPath(
            url,
            allowRedirectToCanonicalPath: true,
            cancellationToken);
    }

    private async Task<PageViewModel> RetrievePageFromPath(
        string url,
        bool allowRedirectToCanonicalPath,
        CancellationToken cancellationToken = default)
    {
        var decodedPath = WebUtility.UrlDecode(url);

        var selected = await ViewModelList.GetPageAsync(decodedPath) as PageViewModel
            ?? throw new ArgumentException($"no settings found for [{decodedPath}]");

        // A newer navigation may have started while awaiting the page data. If so, skip the
        // redirect and path bookkeeping below so we don't override the user's latest navigation.
        if (cancellationToken.IsCancellationRequested)
        {
            return selected;
        }

        if (!allowRedirectToCanonicalPath)
        {
            return selected;
        }

        var selectedKey = selected.Model?.Key
            ?? throw new InvalidOperationException("Key cannot be null");

        var path = ViewModelList.GetPathFromKey(selectedKey, decodedPath);

        if (HasFewerSegments(path, decodedPath))
        {
            // Update _lastLoadedPath to the redirect target so OnParametersSetAsync
            // does not re-fetch the same page when the navigation triggers a new parameter set.
            _lastLoadedPath = EncodeUrlPath(path);
            NavigationManager.NavigateTo($"{PageRoute.Settings}/{EncodeUrlPath(path)}");

            // get the group key for scrolling to the group when navigating to parent
            var groupKey = selected.Items
                .Select(c => c.Model)
                .FirstOrDefault(model => model!.Url == decodedPath)
                ?.Key;

            if (groupKey != null)
            {
                GroupToScroll = groupKey;
            }
        }

        return selected;
    }

    public async Task DeletePageListEntryButtonClicked(PageListViewModel viewModel)
    {
        var modal = Modal.Show<DeletePageListEntryView>(
            new ModalOptions
            {
                Title = Loc["SettingsPage_Delete_Setting_Title"],
                ModalWidth = ModalSize.Small,
                ModalHeight = ModalSize.Small
            }
        );

        var result = await modal.Result;

        if (result.Data is true)
        {
            await DeletePageListEntry(viewModel);
        }
    }

    private async Task DeletePageListEntry(PageListViewModel viewModel)
    {
        var selectedTubeTypeCandidates = GetSelectedTubeTypeDeleteCandidates(viewModel);
        var result = await ViewModelList.DeletePageListEntry(viewModel);

        await _phDataGrid.ClearSelectedItem();
        await ReloadPage();

        if (result is null || result.Skipped <= 0)
        {
            return;
        }

        if (IsTubeTypesPageList(viewModel))
        {
            if (selectedTubeTypeCandidates.Count > 0)
            {
                var tubeTypeOutcome = BuildTubeTypeDeleteOutcome(selectedTubeTypeCandidates, result);

                if (ShouldShowTubeTypeDeleteOutcome(tubeTypeOutcome))
                {
                    var outcomeModal = ShowDeleteTubeTypesOutcomeModal(tubeTypeOutcome);
                    await outcomeModal.Result;
                }
            }

            return;
        }

        var messageConfig = GetDeleteSkippedMessageConfig(viewModel, result);
        var skippedModal = ShowDeleteSkippedModal(messageConfig);
        await skippedModal.Result;
    }

    /// <summary>
    /// Resolves localized title/message resource keys and count argument for skipped page-list deletions.
    /// </summary>
    private static DeleteSkippedMessageConfig GetDeleteSkippedMessageConfig(PageListViewModel viewModel, DeleteResponse result)
    {
        return new DeleteSkippedMessageConfig(
                "SettingsPage_DeleteSkippedTitle",
                "SettingsPage_DeleteSkippedText",
                (viewModel.Model as PageList)?.MinItems ?? result.Skipped);
    }

    /// <summary>
    /// Opens the generic skipped-delete dialog.
    /// </summary>
    private IModalReference ShowDeleteSkippedModal(DeleteSkippedMessageConfig messageConfig)
    {
        var message = string.Format(Loc[messageConfig.MessageResourceKey], messageConfig.MessageArgument);

        return Modal.ShowInformation(
            message,
            Loc["InformationModal_Close"],
            new ModalOptions
            {
                Status = ModalStatus.Warning,
                Title = Loc[messageConfig.TitleResourceKey],
                ModalWidth = ModalSize.Small,
                ModalHeight = ModalSize.Small
            });
    }

    /// <summary>
    /// Opens the tube-type outcome dialog for blocked tube-type deletions.
    /// </summary>
    private IModalReference ShowDeleteTubeTypesOutcomeModal(TubeTypeDeleteOutcome tubeTypeOutcome)
    {
        var parameters = new ModalParameters
        {
            { nameof(DeleteTubeTypesOutcomeView.DeletedEntries), tubeTypeOutcome.DeletedEntries },
            { nameof(DeleteTubeTypesOutcomeView.BlockedEntries), tubeTypeOutcome.BlockedEntries }
        };

        return Modal.Show<DeleteTubeTypesOutcomeView>(
            new ModalOptions
            {
                Status = ModalStatus.Warning,
                Title = Loc["TubeDefinitionsPage_DeleteDialogHeader"],
                ModalWidth = ModalSize.Small,
                ModalHeight = ModalSize.Medium
            },
            parameters);
    }

    private static bool IsTubeTypesPageList(PageListViewModel viewModel)
        => string.Equals((viewModel.Model as PageList)?.Key, _tubeTypesPageListKey, StringComparison.Ordinal);

    /// <summary>
    /// Captures selected tube type entries before deletion so post-delete outcomes can be reported unambiguously.
    /// </summary>
    private static List<TubeTypeDeleteCandidate> GetSelectedTubeTypeDeleteCandidates(PageListViewModel viewModel)
    {
        if (!IsTubeTypesPageList(viewModel))
        {
            return [];
        }

        return viewModel.Values
            .Where(row => row.IsSelected && !string.IsNullOrWhiteSpace(row.EntryKey))
            .Select(row => new TubeTypeDeleteCandidate(
                row.EntryKey!,
                GetTubeTypeDisplayName(row)))
            .ToList();
    }

    /// <summary>
    /// Resolves a user-facing tube type label for delete-result messages.
    /// </summary>
    private static string GetTubeTypeDisplayName(TableRowViewModel row)
    {
        var label = row.Values
            .FirstOrDefault(value => value.Model?.Key.EndsWith(":Name", StringComparison.Ordinal) == true)
            ?.Value;

        return string.IsNullOrWhiteSpace(label)
            ? row.EntryKey ?? string.Empty
            : label;
    }

    /// <summary>
    /// Builds a detailed tube type deletion outcome from selected rows and server-reported skipped keys.
    /// </summary>
    internal static TubeTypeDeleteOutcome BuildTubeTypeDeleteOutcome(
        IEnumerable<TubeTypeDeleteCandidate> selectedCandidates,
        DeleteResponse response)
    {
        var selectedList = selectedCandidates.ToList();
        var skippedReasonByEntryKey = (response.SkippedTubeTypeReasons ?? [])
            .Where(reason => !string.IsNullOrWhiteSpace(reason.PageListEntryKey))
            .GroupBy(reason => reason.PageListEntryKey, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.Ordinal);
        var skippedEntryKeys = response.SkippedPageListEntryKeys?.ToHashSet(StringComparer.Ordinal) ?? [];

        skippedEntryKeys.UnionWith(skippedReasonByEntryKey.Keys);

        var blockedEntries = selectedList
            .Where(candidate => skippedEntryKeys.Contains(candidate.EntryKey))
            .OrderBy(candidate => candidate.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(candidate => new DeleteTubeTypesOutcomeView.TubeTypeDeleteBlockedEntry(
                candidate.DisplayName,
                GetTubeTypeDeleteBlockedReasonResourceKey(candidate.EntryKey, skippedReasonByEntryKey)))
            .ToList();

        var deletedEntries = selectedList
            .Where(candidate => !skippedEntryKeys.Contains(candidate.EntryKey))
            .OrderBy(candidate => candidate.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(candidate => candidate.DisplayName)
            .ToList();

        return new TubeTypeDeleteOutcome(
            selectedList.Count,
            response.Deleted,
            response.Skipped,
            deletedEntries,
            blockedEntries);
    }

    internal static bool ShouldShowTubeTypeDeleteOutcome(TubeTypeDeleteOutcome outcome)
        => outcome.BlockedEntries.Count > 0;

    private static string GetTubeTypeDeleteBlockedReasonResourceKey(
        string entryKey,
        Dictionary<string, DeleteSkippedTubeTypeReason> skippedReasonByEntryKey)
    {
        if (skippedReasonByEntryKey.TryGetValue(entryKey, out var skippedReason))
        {
            return skippedReason switch
            {
                { IsDefaultTubeType: true } => "SettingsSamples_SetAsDefaultTubeType",
                { UsedByRequests: true, UsedByQualityControls: true } => "SettingsSamples_UsedByRequestsAndQualityControls",
                { UsedByRequests: true, UsedByQualityControls: false } => "SettingsSamples_UsedByRequests",
                { UsedByRequests: false, UsedByQualityControls: true } => "SettingsSamples_UsedByQualityControls",
                _ => "SettingsSamples_CouldNotDelete"
            };
        }

        return "SettingsSamples_CouldNotDelete";
    }

    private record DeleteSkippedMessageConfig(string TitleResourceKey, string MessageResourceKey, int MessageArgument);

    internal record TubeTypeDeleteCandidate(string EntryKey, string DisplayName);

    internal record TubeTypeDeleteOutcome(
        int SelectedCount,
        int DeletedCount,
        int SkippedCount,
        List<string> DeletedEntries,
        List<DeleteTubeTypesOutcomeView.TubeTypeDeleteBlockedEntry> BlockedEntries);

    private async Task<bool> Save()
    {
        if (_isSaving)
        {
            return false;
        }

        var wasLocalPageBeforeSave = ViewModelList.IsLocalPage(Path);

        _isSaving = true;
        _navigationDeferredDuringSave = false;
        _targetUrl = null;
        try
        {
            var modifiedItemsBeforeSave = Selected!.Items
                .Where(item => item.IsDependencyEnabled && item.IsModified)
                .ToList();

            Selected.Validate();

            if (Selected!.IsValid)
            {
                Selected!.Save();
                var isPageListUpdate = !ViewModelList.IsLocalPage(Path);
                var saveResult = await ViewModelList.Update(Selected.Model!, isPageListUpdate);
                if (!saveResult.Success)
                {
                    // Reset all modified settings back to modified as backend validation failed
                    foreach (var entry in modifiedItemsBeforeSave)
                    {
                        entry.IsModified = true;
                    }
                    Selected.IsModified = true;
                    Selected.IsLocal = wasLocalPageBeforeSave;

                    var pageListError = saveResult.Errors?.FirstOrDefault(e =>
                        e.Error == SettingUpdateError.MaxItemsExceeded ||
                        e.Error == SettingUpdateError.ConcurrentModification);
                    var writeAccessError = saveResult.Errors?.FirstOrDefault(e =>
                        e.Error == SettingUpdateError.AbsoluteDirectoryWriteAccessDenied);
                    var validationErrors = saveResult.Errors?
                        .Where(e => e.Error is SettingUpdateError.ValidationError or SettingUpdateError.SectionsOverlap)
                        .ToList() ?? [];

                    if (writeAccessError != null)
                    {
                        var errorMessage = string.Format(
                            Loc["Validation_AbsoluteDirectoryWriteAccessDenied"],
                            writeAccessError.Value);

                        ToasterService.ShowToast(ToastType.Warning, errorMessage);
                    }
                    else if (pageListError != null)
                    {
                        // Determine which error message to show
                        string errorMessage;

                        if (pageListError.Error == SettingUpdateError.ConcurrentModification)
                        {
                            errorMessage = Loc["Validation_ConcurrentCreationConflict"];
                        }
                        else
                        {
                            // Extract PageList key from the current page's key
                            var pageKey = Selected.Model!.Key;
                            var segments = pageKey.Split(':');
                            var pageListKey = string.Join(":", segments.Take(segments.Length - 1));
                            var rootPageLabel = segments[0];

                            // Get max items limit from server
                            var checkResult = await ViewModelList.CheckPageListMaxItemsAsync(pageListKey, rootPageLabel);

                            errorMessage = checkResult.MaxItems.HasValue
                                ? string.Format(Loc["Validation_PageListMaxItemsReached"], checkResult.MaxItems.Value)
                                : Loc["Validation_PageListMaxItemsReached"].Value.Replace(" ({0})", "");
                        }

                        ToasterService.ShowToast(ToastType.Warning, errorMessage);
                    }
                    else if (validationErrors.Count > 0)
                    {
                        Selected.ApplyServerValidationErrors(validationErrors);
                        await ShowSaveSettingsErrorDialog();
                    }
                    else
                    {
                        // Show generic validation error dialog for other errors
                        await ShowSaveSettingsErrorDialog();
                    }

                    return false;
                }
                await ReloadPage();
                ToasterService.ShowToast(ToastType.Confirmation, Loc["SettingsSave_Successful_InfoToaster"]);
                await UiSettingsService.NotifySettingsChangedAsync();
                return true;
            }
            else
            {
                await ShowSaveSettingsErrorDialog();
                return false;
            }
        }
        catch (ApiException e) when (e.StatusCode == (int)HttpStatusCode.Gone)
        {
            _navigationDeferredDuringSave = false;
            _targetUrl = null;

            ExceptionHandlerService.ShowApiException(e, ExceptionDisplayMode.ModalKeepOnNavigation);
            await PrepareNavigationAfterDeletedPage();

            return false;
        }
        finally
        {
            _isSaving = false;

            var processedPendingDeletion = await ProcessPendingExternalDeletionAsync();

            if (processedPendingDeletion)
            {
                _navigationDeferredDuringSave = false;
                _targetUrl = null;
            }

            // If navigation was attempted during save, navigate only after the save succeeded.
            if (!processedPendingDeletion &&
                _navigationDeferredDuringSave &&
                !string.IsNullOrEmpty(_targetUrl))
            {
                var targetUrl = _targetUrl;
                _targetUrl = null;
                _navigationDeferredDuringSave = false;
                NavigationManager.NavigateTo(targetUrl);
            }

        }
    }

    private async Task PrepareNavigationAfterDeletedPage()
    {
        var deletedPagePath = WebUtility.UrlDecode(Path);

        ClearDeletedSelectedPage();

        try
        {
            await ReloadPageRouteMap();
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "Failed to refresh settings route map after deleted settings save for Path={Path}",
                Path);
        }

        _lastLoadedPath = null;
        _deletedPageNavigationPath = deletedPagePath;
        StateHasChanged();
    }

    private void ClearDeletedSelectedPage()
    {
        if (Selected is not null)
        {
            MarkAsUnmodified(Selected);
        }

        Selected = null;
        Items = [];
        ShowBreadcrumbs = false;
    }

    private static void MarkAsUnmodified(SettingsViewModelBase item)
    {
        if (item is ContainerSettingsViewModelBase container)
        {
            foreach (var child in container.Items)
            {
                MarkAsUnmodified(child);
            }
        }

        item.ClearValidationErrors();
        item.IsModified = false;
    }

    private async Task InitializeBreadcrumbs()
    {
        Items = [];
        var textInfo = CultureInfo.InvariantCulture.TextInfo;
        var breadcrumbHref = $"{NavigationManager.BaseUri}{PageRoute.Settings}";
        var baseItem = new BreadcrumbItem(textInfo.ToTitleCase($"{RouteItemBase.Settings}"), breadcrumbHref);
        Items.Add(baseItem);
        var settingsPath = string.Empty;
        foreach (var segment in Path?.Split("/") ?? [])
        {
            var decodedSegment = WebUtility.UrlDecode(segment);

            settingsPath = AppendSettingsPathSegment(settingsPath, decodedSegment);
            var breadcrumbText = await GetBreadcrumbDisplayText(decodedSegment, settingsPath);
            breadcrumbHref = $"{breadcrumbHref}/{Uri.EscapeDataString(decodedSegment)}";

            var breadcrumbItem = new BreadcrumbItem(breadcrumbText, breadcrumbHref);
            Items.Add(breadcrumbItem);
        }
    }

    private static string AppendSettingsPathSegment(string currentPath, string nextSegment)
    {
        return string.IsNullOrWhiteSpace(currentPath) ? nextSegment : $"{currentPath}/{nextSegment}";
    }

    private async Task<string> GetBreadcrumbDisplayText(string decodedSegment, string settingsPath)
    {
        if (!IsPageListEntryIdSegment(decodedSegment))
        {
            return decodedSegment;
        }

        var isSelectedPage = Selected != null && string.Equals(Selected.Url, settingsPath, StringComparison.Ordinal);
        if (isSelectedPage && Selected!.IsLocal)
        {
            // Display "New" for unsaved new entries, since they won't have a label yet
            return Loc["PageList_NewEntryLabel"];
        }

        // PageList entry URLs intentionally use IDs, but breadcrumbs should remain user-facing and show labels.
        var label = isSelectedPage
            ? Selected!.Label

            // For pages that are not currently selected but are still segmented with an ID as part of the path
            // (e.g. <hostname>/more/settings/LIS/5/ASTM%20settings/Communication -> Settings/LIS/LIS Ch5/ASTM Settings/Communication)
            : (await ViewModelList.GetPageAsync(settingsPath)).Label;

        return string.IsNullOrWhiteSpace(label) ? decodedSegment : label;
    }

    private static bool IsPageListEntryIdSegment(string segment)
    {
        return int.TryParse(segment, NumberStyles.None, CultureInfo.InvariantCulture, out _);
    }

    private async Task ShowSaveSettingsErrorDialog()
    {
        var modal = Modal.Show<SaveSettingsErrorView>(new ModalOptions { Title = Loc["SettingsSaveModal_Title"], ModalHeight = ModalSize.Small });
        // Wait for the modal to close
        await modal.Result;
    }

    private async Task DiscardChanges()
    {
        if (ViewModelList.IsLocalPage(Path))
        {
            // Reset general settings
            ResetToDefault(Selected!.Items);

            // Reset group settings
            foreach (var item in Selected.Items)
            {
                if (item is GroupViewModel groupViewModel)
                {
                    ResetToDefault(groupViewModel.Items);
                }
            }

            Selected.ClearValidationErrors();
            return;
        }

        await ReloadPage();
    }

    private async Task ReloadPage()
    {
        var selectedKey = Selected?.Model?.Key
            ?? throw new InvalidOperationException("Key cannot be null");

        await ReloadPageRouteMap();

        var decodedPath = WebUtility.UrlDecode(Path);

        if (!ViewModelList.TryGetPathFromKey(selectedKey, decodedPath, out var path))
        {
            NavigateToSettingsRootPath(decodedPath);
            return;
        }

        if (string.Equals(path, decodedPath, StringComparison.Ordinal))
        {
            Selected = await RetrievePageFromPath(Path);

            await InitializeBreadcrumbs();

            StateHasChanged();
        }
        else
        {
            NavigationManager.NavigateTo(
                $"{PageRoute.Settings}/{EncodeUrlPath(path)}");
        }
    }

    private void OnExternalSettingsChanged(object? sender, SettingsChangedEventArgs e)
    {
        _ = InvokeAsync(() => QueueExternalRefreshAsync(e));
    }

    private async Task QueueExternalRefreshAsync(SettingsChangedEventArgs change)
    {
        if (_disposed)
        {
            return;
        }

        if (_isSaving)
        {
            StorePendingCurrentPageDeletion(change);
            return;
        }

        if (_isHandlingExternalRefresh)
        {
            if (StorePendingCurrentPageDeletion(change))
            {
                CancelAndDispose(ref _externalRefreshCts);
            }

            return;
        }

        var currentPageDeleted = await ShowExternalDeletionDialogIfNeeded(change);
        if (currentPageDeleted)
        {
            return;
        }

        if (Selected?.IsModified == true)
        {
            var currentPageUpdated = await ShowExternalModificationDialogIfNeeded(change);
            if (currentPageUpdated)
            {
                return;
            }

            if (ShouldRefreshRouteMap(change))
            {
                await RefreshRouteMapForExternalChangeSafelyAsync(CancellationToken.None);
            }

            return;
        }

        var currentPageAffected = IsCurrentPageAffected(change);
        var shouldRefreshRouteMap = ShouldRefreshRouteMap(change);
        if (!currentPageAffected && !shouldRefreshRouteMap)
        {
            return;
        }

        _isHandlingExternalRefresh = true;

        try
        {
            CancelAndDispose(ref _externalRefreshCts);

            var cts = new CancellationTokenSource();
            _externalRefreshCts = cts;

            try
            {
                await Task.Delay(250, cts.Token);

                if (currentPageAffected)
                {
                    await ReloadPageForExternalChangeAsync(cts.Token);
                }
                else
                {
                    await RefreshRouteMapForExternalChangeAsync(cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    ex,
                    "External settings refresh failed for Path={Path}",
                    Path);
            }
        }
        finally
        {
            _isHandlingExternalRefresh = false;
            await ProcessPendingExternalDeletionAsync();
        }
    }

    private static void CancelAndDispose(
        ref CancellationTokenSource? cts)
    {
        var current = Interlocked.Exchange(ref cts, null);

        if (current is null)
        {
            return;
        }

        try
        {
            current.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Another path already disposed it.
        }

        current.Dispose();
    }

    private async Task ReloadPageForExternalChangeAsync(
        CancellationToken cancellationToken)
    {
        if (Selected?.Model?.Key is null)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        await RefreshRouteMapForExternalChangeAsync(cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        var refreshPath = Path;

        // Reuse the existing page-load cancellation protection.
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        var loadCts = CancellationTokenSource
            .CreateLinkedTokenSource(cancellationToken);
        _loadCts = loadCts;

        // SignalR refreshes should update the current view without normalizing
        // group URLs back to the root settings page.
        var selected = await RetrievePageFromPath(
            refreshPath,
            allowRedirectToCanonicalPath: false,
            loadCts.Token);

        if (loadCts.IsCancellationRequested ||
            !ReferenceEquals(_loadCts, loadCts) ||
            !string.Equals(Path, refreshPath, StringComparison.Ordinal))
        {
            return;
        }

        Selected = selected;

        await InitializeBreadcrumbs();

        cancellationToken.ThrowIfCancellationRequested();

        StateHasChanged();
    }

    private async Task RefreshRouteMapForExternalChangeAsync(
        CancellationToken cancellationToken)
    {
        await ReloadPageRouteMap(cancellationToken).WaitAsync(
            TimeSpan.FromSeconds(5),
            cancellationToken);
    }

    private async Task RefreshRouteMapForExternalChangeSafelyAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await RefreshRouteMapForExternalChangeAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "External settings route map refresh failed for Path={Path}",
                Path);
        }
    }

    private bool StorePendingCurrentPageDeletion(SettingsChangedEventArgs change)
    {
        if (!IsCurrentPageDeletion(change))
        {
            return false;
        }

        _pendingExternalDeletionChange = change;
        return true;
    }

    private bool HasPendingCurrentPageDeletion()
    {
        return _pendingExternalDeletionChange is not null &&
            IsCurrentPageDeletion(_pendingExternalDeletionChange);
    }

    private bool IsCurrentPageDeletion(SettingsChangedEventArgs change)
    {
        var selectedKey = Selected?.Model?.Key;
        return !string.IsNullOrWhiteSpace(selectedKey) &&
            IsCurrentPageDeleted(change, selectedKey);
    }

    private async Task<bool> ProcessPendingExternalDeletionAsync()
    {
        if (_disposed ||
            _isSaving ||
            _isHandlingExternalRefresh ||
            _externalSettingsChangeModalOpen ||
            _pendingExternalDeletionChange is null)
        {
            return false;
        }

        var pendingDeletion = _pendingExternalDeletionChange;
        _pendingExternalDeletionChange = null;

        await QueueExternalRefreshAsync(pendingDeletion);
        return true;
    }

    private async Task<bool> ShowExternalDeletionDialogIfNeeded(SettingsChangedEventArgs change)
    {
        var selectedKey = Selected?.Model?.Key;
        if (string.IsNullOrWhiteSpace(selectedKey) ||
            !IsCurrentPageDeleted(change, selectedKey))
        {
            return false;
        }

        if (_externalSettingsChangeModalOpen)
        {
            StorePendingCurrentPageDeletion(change);
            return true;
        }

        _pendingExternalDeletionChange = null;
        _externalSettingsChangeModalOpen = true;

        try
        {
            var modal = Modal.ShowExternalDataChangeDialog(
                Loc,
                ExternalDataChangeKind.Deleted,
                Loc["SettingsRefresh_SettingDialogTitleEntity"],
                change.ChangedBy,
                Loc["SettingsRefresh_DeletedPageDialogText"],
                Loc["SettingsRefresh_BackToSettings"]);

            await modal.Result;

            if (_disposed)
            {
                return true;
            }

            await PrepareNavigationAfterDeletedPage();
        }
        finally
        {
            _externalSettingsChangeModalOpen = false;
            await ProcessPendingExternalDeletionAsync();
        }

        return true;
    }

    private async Task<bool> ShowExternalModificationDialogIfNeeded(SettingsChangedEventArgs change)
    {
        var selectedKey = Selected?.Model?.Key;
        if (string.IsNullOrWhiteSpace(selectedKey) ||
            IsCurrentPageDeleted(change, selectedKey) ||
            !IsCurrentPageAffected(change))
        {
            return false;
        }

        if (_externalSettingsChangeModalOpen)
        {
            return true;
        }

        _externalSettingsChangeModalOpen = true;

        try
        {
            var modal = Modal.ShowExternalDataChangeDialog(
                Loc,
                ExternalDataChangeKind.Updated,
                Loc["SettingsRefresh_SettingDialogTitleEntity"],
                change.ChangedBy,
                Loc["SettingsRefresh_UpdatedPageDialogText"],
                Loc["SettingsRefresh_RefreshSettings"]);

            await modal.Result;

            if (_disposed)
            {
                return true;
            }

            if (HasPendingCurrentPageDeletion())
            {
                return true;
            }

            await ReloadPageForExternalChangeAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "Failed to refresh dirty settings page after external update for Path={Path}",
                Path);
        }
        finally
        {
            _externalSettingsChangeModalOpen = false;
            await ProcessPendingExternalDeletionAsync();
        }

        return true;
    }

    private bool IsCurrentPageAffected(SettingsChangedEventArgs change)
    {
        var selectedKey = Selected?.Model?.Key;
        if (string.IsNullOrWhiteSpace(selectedKey) || !change.HasKeyDetails)
        {
            return true;
        }

        return IsCurrentPageUpdated(change, selectedKey) ||
            change.DeletedKeys.Any(key => IsSameOrRelatedSettingsKey(key, selectedKey));
    }

    private static bool ShouldRefreshRouteMap(SettingsChangedEventArgs change)
    {
        return change.HasKeyDetails && change.DeletedKeys.Count > 0;
    }

    private static bool IsCurrentPageUpdated(SettingsChangedEventArgs change, string selectedKey)
    {
        return !change.HasKeyDetails ||
            change.UpdatedKeys.Any(key => IsSameOrRelatedSettingsKey(key, selectedKey));
    }

    private static bool IsCurrentPageDeleted(SettingsChangedEventArgs change, string selectedKey)
    {
        return change.DeletedKeys.Any(key =>
            string.Equals(key, selectedKey, StringComparison.Ordinal) ||
            selectedKey.StartsWith(key + _settingsKeyDelimiter, StringComparison.Ordinal));
    }

    private static bool IsSameOrRelatedSettingsKey(string changedKey, string selectedKey)
    {
        return string.Equals(changedKey, selectedKey, StringComparison.Ordinal) ||
            changedKey.StartsWith(selectedKey + _settingsKeyDelimiter, StringComparison.Ordinal) ||
            selectedKey.StartsWith(changedKey + _settingsKeyDelimiter, StringComparison.Ordinal);
    }

    private void NavigateToSettingsRootPath(string decodedPath)
    {
        var rootPath = GetRootSettingsPath(decodedPath);
        var target = string.IsNullOrWhiteSpace(rootPath)
            ? PageRoute.Settings
            : $"{PageRoute.Settings}/{EncodeUrlPath(rootPath)}";

        NavigationManager.NavigateTo(target);
    }

    private static string GetRootSettingsPath(string path)
    {
        return path.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
    }

    private Task ReloadPageRouteMap()
    {
        return ReloadPageRouteMap(CancellationToken.None);
    }

    private async Task ReloadPageRouteMap(
        CancellationToken cancellationToken = default)
    {
        var pageRouteMap =
            await ViewModelList.RefreshPageRouteMap(cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        ViewModelList.SetPageRouteMap(pageRouteMap);
    }

    private static void ResetToDefault(List<SettingsViewModelBase> items)
    {
        foreach (var item in items)
        {
            if (item.Model is Option { IsHidden: true } or Option { IsReadOnly: true })
            {
                continue;
            }

            item.ResetToDefault();
        }
    }

    private static bool HasFewerSegments(string path1, string path2)
    {
        var segments1 = path1.Split('/');
        var segments2 = path2.Split('/');

        return segments1.Length < segments2.Length;
    }

    private static string EncodeUrlPath(string path) =>
        string.Join('/', path.Split('/').Select(Uri.EscapeDataString));

    public override async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        UiSettingsService.SettingsChanged -= OnSettingsChangedAsync;
        SettingsNotificationProvider.SettingsChanged -= OnExternalSettingsChanged;

        if (_phDataGrid is not null)
        {
            await _phDataGrid.DisposeAsync();
        }

        await _script.SafeDisposeAsync();
        await DisposeSettingsActionScriptsAsync();

        _registration?.Dispose();

        _externalRefreshCts?.Cancel();
        _externalRefreshCts?.Dispose();

        _loadCts?.Cancel();
        _loadCts?.Dispose();

        GC.SuppressFinalize(this);
    }

    public async Task GoToSubSettings(string localUri)
    {
        if (_isSaving)
        {
            return;
        }

        await ViewModelList.GoToSubSettings(Path, localUri);
    }

    private string GetBooleanLabel(bool value)
    {
        return value
            ? Loc["Settings_Yes"]
            : Loc["Settings_No"];
    }
    private string GetBooleanLabel(string? value)
        => GetBooleanLabel(bool.TryParse(value, out var parsed) && parsed);

    private async Task ExecuteAction(ActionButtonViewModel viewModel)
    {
        var action = GetSettingsAction(viewModel);

        if (!await ConfirmSettingsAction(action))
        {
            return;
        }

        var result = await action.ExecuteAsync(_settingsActionScripts);
        await ApplySettingsActionResult(result);
    }

    private IconType? GetActionButtonIcon(ActionButtonViewModel actionButtonViewModel)
    {
        return _settingsActions.TryGetValue(actionButtonViewModel.ActionKey, out var action)
            ? action.StartIconType
            : null;
    }

    private static ButtonType GetActionButtonType(ActionButtonViewModel actionButtonViewModel)
    {
        return actionButtonViewModel.ButtonType switch
        {
            ActionButtonType.Primary => ButtonType.Primary,
            ActionButtonType.Secondary => ButtonType.Secondary,
            ActionButtonType.Text => ButtonType.Text,
            _ => ButtonType.Secondary
        };
    }

    private ISettingsAction GetSettingsAction(ActionButtonViewModel actionButtonViewModel)
    {
        if (_settingsActions.TryGetValue(actionButtonViewModel.ActionKey, out var action))
        {
            return action;
        }

        throw new ArgumentOutOfRangeException(nameof(actionButtonViewModel), actionButtonViewModel.ActionKey, "Unsupported action key.");
    }

    private async Task LoadSettingsActionScripts()
    {
        var requiredScripts = _settingsActions.Values
            .SelectMany(action => action.RequiredScripts)
            .Distinct();

        foreach (var script in requiredScripts)
        {
            if (_settingsActionScripts.ContainsKey(script))
            {
                continue;
            }

            _settingsActionScripts[script] = await ImportSettingsActionScript(script);
        }
    }

    private Task<IJSObjectReference> ImportSettingsActionScript(SettingsActionScript script)
    {
        return script.Source switch
        {
            SettingsActionScriptSource.SettingsModule => ImportJSAsync(script.FileName),
            SettingsActionScriptSource.CommonWeb => ImportCommonUtilityJSAsync(script.FileName),
            _ => throw new ArgumentOutOfRangeException(nameof(script), script.Source, "Unsupported settings action script source.")
        };
    }

    private async ValueTask DisposeSettingsActionScriptsAsync()
    {
        foreach (var script in _settingsActionScripts.Values)
        {
            await script.SafeDisposeAsync();
        }

        _settingsActionScripts.Clear();
    }

    private async Task<bool> ConfirmSettingsAction(ISettingsAction action)
    {
        return action.Confirmation?.Type switch
        {
            null => true,
            SettingsActionConfirmationType.DeleteExistingSettings => await ShowDeleteExistingSettingsPrompt(),
            _ => throw new ArgumentOutOfRangeException(nameof(action), action.Confirmation.Type, "Unsupported settings action confirmation.")
        };
    }

    private async Task<bool> ShowDeleteExistingSettingsPrompt()
    {
        var modal = Modal.Show<DeleteExistingSettingsView>(new ModalOptions { Title = Loc["SettingsImport_DeleteExistingSettings_Dialog_Title"] });
        var result = await modal.Result;
        return result.Data is true;
    }

    private async Task ApplySettingsActionResult(SettingsActionResult result)
    {
        foreach (var effect in result.Effects)
        {
            switch (effect)
            {
                case SettingsActionToastEffect toast:
                    ToasterService.ShowToast(GetToastType(toast.Type), Loc[toast.MessageResourceName]);
                    break;
                case SettingsActionExportResultDialogEffect exportResultDialog:
                    await ShowSettingsExportResultDialog(exportResultDialog);
                    break;
                case SettingsActionImportResultDialogEffect importResultDialog:
                    await ShowSettingsImportResultDialog(importResultDialog);
                    break;
                case SettingsActionNavigateToSettingsRootEffect:
                    NavigationManager.NavigateTo($"{PageRoute.Settings}");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(result), effect, "Unsupported settings action effect.");
            }
        }
    }

    private static ToastType GetToastType(SettingsActionToastType toastType)
    {
        return toastType switch
        {
            SettingsActionToastType.Information => ToastType.Information,
            SettingsActionToastType.Warning => ToastType.Warning,
            SettingsActionToastType.Confirmation => ToastType.Confirmation,
            SettingsActionToastType.Alarm => ToastType.Alarm,
            _ => ToastType.Information
        };
    }

    private async Task ShowSettingsExportResultDialog(SettingsActionExportResultDialogEffect effect)
    {
        var parameters = new ModalParameters
        {
            { nameof(ExportSettingsResultView.ErrorMessages), new[] { Loc[effect.ErrorMessageResourceName].Value } }
        };
        var modal = Modal.Show<ExportSettingsResultView>(new ModalOptions { Title = Loc["SettingsExport_Dialog_Title"], Status = ModalStatus.Warning }, parameters);
        await modal.Result;
    }

    private async Task ShowSettingsImportResultDialog(SettingsActionImportResultDialogEffect effect)
    {
        var parameters = new ModalParameters
        {
            { nameof(ImportSettingsResultView.IsErrorInvalidFile), effect.IsErrorInvalidFile },
            { nameof(ImportSettingsResultView.InvalidSettings), effect.InvalidSettings },
            { nameof(ImportSettingsResultView.IsUnrecognizedSettings), effect.IsUnrecognizedSettings },
            { nameof(ImportSettingsResultView.UnrecognizedSettings), effect.UnrecognizedSettings }
        };
        var modal = Modal.Show<ImportSettingsResultView>(new ModalOptions { Title = Loc["SettingsImport_Dialog_Title"], Status = ModalStatus.Warning }, parameters);
        await modal.Result;
    }
}
