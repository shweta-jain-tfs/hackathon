using Microsoft.AspNetCore.Components.Forms;
using Pharos.Client.App.Request.Models;
using Pharos.Client.App.Request.ViewModels;
using Pharos.Client.App.Request.Views.RequestList;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.Services;
using Pharos.Common.Web.Components.DataGrid;
using Pharos.Common.Web.Components.Modal;
using Pharos.Common.Web.Components.Status;
using Pharos.Common.Web.Components.Toaster;
using Pharos.Common.Web.Components.UploadFile;
using Pharos.Contracts.Security;

namespace Pharos.Client.App.Request.Views;

public partial class RequestListPage : IDisposable
{
    [Inject] private HttpClient HttpClient { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private RequestListViewModel ViewModel { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IModalService ModalService { get; set; } = default!;
    [Inject] private IRequestDetailsDataService RequestDetailsDataService { get; set; } = default!;
    [Inject] private UiSettingsService UiSettingsService { get; set; } = default!;
    [Inject] private IAuthorizationService AuthorizationService { get; set; } = default!;
    [Inject] private IToasterService ToasterService { get; set; } = default!;

    private PhDataGrid<RequestSummary> _phDataGrid = default!;

    private bool HasPatientPermission =>
        HasViewPersonalInfoPermission &&
        _uiSettings != null && _uiSettings.RequestSettings.IsPatientInformationEnabled;

    private bool HasRequestorPermission =>
        HasViewPersonalInfoPermission &&
        _uiSettings != null && _uiSettings.RequestSettings.IsRequestorInformationEnabled;

    private bool HasViewPersonalInfoPermission { get; set; }
    private bool HasManageTestOrdersPermission { get; set; }

    private HashSet<RequestSummary>? _selectedItems;
    private string TextPrimaryButton { get; set; } = string.Empty;

    private UiSettings? _uiSettings;
    private PhUploadFiles? _uploadFiles;
    private bool _canImportRequests;

    protected override void OnInitialized()
    {
        ViewModel.ViewRefreshNeeded += OnViewRefreshNeeded;
    }

    protected override async Task OnInitializedAsync()
    {
        _uiSettings = await UiSettingsService.GetSettingsAsync();
        HasViewPersonalInfoPermission = await AuthorizationService.IsAuthorized(Permissions.ViewPersonalAndHealthData);
        HasManageTestOrdersPermission = await AuthorizationService.IsAuthorized(Permissions.ManageTestOrders);
        _canImportRequests = await AuthorizationService.IsRndMode();

        UpdateButtonTexts();
    }

    private bool HasSelectedItems => _selectedItems is { Count: > 0 };

    private void OnViewRefreshNeeded(object? sender, EventArgs e)
    {
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        ViewModel.ViewRefreshNeeded -= OnViewRefreshNeeded;
        ViewModel.Dispose();

        // _phDataGrid is null if exception occurs during OnInitializedAsync
        // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
        _phDataGrid?.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task DeleteRequestsOrOpenDialog()
    {
        if (_selectedItems is { Count: > 0 } && _selectedItems.All(x => x.State is RequestState.NotStarted))
        {
            // Delete without confirmation dialog if select all was not used and all statuses are "NotStarted"
            await DeleteSelectedRows();
        }
        else
        {
            // Confirmation dialog.
            var parameters = new ModalParameters
            {
                {nameof(RequestListDeleteView.PrimaryButtonText), TextPrimaryButton}
            };

            var modal = ModalService.Show<RequestListDeleteView>(
                new ModalOptions
                {
                    Title = Loc["RequestListPage_DeleteDialogHeader"],
                    ModalHeight = ModalSize.XSmall,
                    ModalWidth = ModalSize.Small
                }, parameters);

            var result = await modal.Result;

            if (!result.Canceled)
            {
                await DeleteSelectedRows();
            }
        }
    }


    private async Task DeleteSelectedRows()
    {
        if (_selectedItems != null)
        {
            await ViewModel.DeleteSamples(_selectedItems!.Select(x => x.Id).ToArray());

            //If we want to keep some selected items after deletion, we can add them to _selectionDirective.ClearSelectedItemsExcept before clearing and then the DataGrid will
            //clear all items except these and send them back.
            await _phDataGrid.ClearSelectedItem();

            _selectedItems!.Clear();
        }
    }

    private void SelectedItemsChanged(HashSet<RequestSummary> selectedItems)
    {
        _selectedItems = selectedItems;
        UpdateButtonTexts();
    }

    private void AddRequest()
    {
        Navigation.NavigateTo($"{PageRoute.SampleDetails.Default}");
    }

    private int SelectionCount()
    {
        return _selectedItems?.Count ?? 0;
    }

    private async Task QueryLisAll()
    {
        await RequestDetailsDataService.QueryLisAsync(Array.Empty<string>());
    }

    private async Task QueryLisSelected()
    {
        if (_selectedItems is null || _selectedItems.Count == 0)
        {
            return;
        }

        var sampleIds = _selectedItems.Select(x => x.SampleId).ToArray();

        if (sampleIds.Length == 0)
        {
            return;
        }

        await RequestDetailsDataService.QueryLisAsync(sampleIds);
    }

    private async Task ExportToLis()
    {
        if (_selectedItems is null || _selectedItems.Count == 0)
        {
            return;
        }

        var requestIds = _selectedItems.Select(x => x.Id).ToArray();

        if (requestIds.Length == 0)
        {
            return;
        }

        await RequestDetailsDataService.ExportToLisAsync(requestIds);
    }

    private async Task ImportPackage_OpenFileDialog()
    {
        if (_uploadFiles is not null)
        {
            await _uploadFiles.OpenFileDialog();
        }
    }

    private async Task ImportPackage_UploadFile(InputFileChangeEventArgs e)
    {
        try
        {
            var file = e.File;
            var client = new TubeDefinitionsClient(HttpClient);

            await using var readStream = file.OpenReadStream(file.Size);
            var fileParameter = new FileParameter(readStream, file.Name);
            await client.RequestImportAsync(fileParameter);

            ToasterService.ShowToast(ToastType.Confirmation, Loc["RequestListPage_ImportRequestsSuccessMessage"]);
        }
        catch (ApiException<ValidationProblemDetails> apiException)
        {
            var (exceptionName, parameterName, details) = ParseImportError(apiException.Result);
            ShowImportErrorDialog(exceptionName, parameterName, details);
        }
        catch
        {
            ToasterService.ShowToast(ToastType.Alarm, Loc["RequestListPage_ImportRequestsErrorMessage"]);
        }
    }

    private void ShowImportErrorDialog(string exceptionName, string parameterName, string details)
    {
        var message = string.Join(Environment.NewLine,
            [
                Loc["RequestListPage_ImportRequestsErrorMessage"].ToString(),
                $"{Loc["RequestListPage_ImportError_Exception"]}: {exceptionName}",
                $"{Loc["RequestListPage_ImportError_Parameter"]}: {parameterName}",
                $"{Loc["RequestListPage_ImportError_Details"]}: {details}"
            ]);

        var modalOptions = new ModalOptions
        {
            Title = Loc["RequestListPage_ImportError_Title"],
            Status = ModalStatus.Alarm,
            ModalHeight = ModalSize.Medium,
            ModalWidth = ModalSize.Medium
        };

        ModalService.ShowInformation(message, Loc["SettingsSaveModal_Close"], modalOptions, false);
    }

    private static (string ExceptionName, string ParameterName, string Details) ParseImportError(ValidationProblemDetails? problemDetails)
    {
        const string fallbackExceptionName = "ApiException";

        if (problemDetails is null)
        {
            return (fallbackExceptionName, string.Empty, string.Empty);
        }

        var title = problemDetails.Title ?? string.Empty;

        if (!TryGetFirstValidationError(problemDetails, out var parameterName, out var details))
        {
            return (fallbackExceptionName, string.Empty, title);
        }

        return ("ArgumentException", parameterName, string.IsNullOrWhiteSpace(details) ? title : details);
    }

    private static bool TryGetFirstValidationError(ValidationProblemDetails problemDetails, out string parameterName, out string details)
    {
        parameterName = string.Empty;
        details = string.Empty;

        if (problemDetails.Errors is null || problemDetails.Errors.Count == 0)
        {
            return false;
        }

        foreach (var error in problemDetails.Errors)
        {
            parameterName = error.Key;
            details = error.Value?.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty;
            return true;
        }

        return false;
    }

    private async Task TestsResetButtonClicked()
    {
        if (_selectedItems is null || _selectedItems.Count == 0)
        {
            return;
        }

        var requestIds = _selectedItems.Select(x => x.Id).ToArray();

        var parameters = new ModalParameters
        {
            {nameof(RequestListResetView.RequestIds), requestIds}
        };

        var modal = ModalService.Show<RequestListResetView>(
            new ModalOptions
            {
                Title = Loc["RequestListPage_ResetDialogHeader"],
                Status = ModalStatus.Info,
                ModalHeight = ModalSize.Medium,
                ModalWidth = ModalSize.Small,
            }, parameters);

        var result = await modal.Result;

        if (result is { Canceled: false, Data: ResetOptions resetOptions })
        {
            await RequestDetailsDataService.ResetTestOrdersAsync(requestIds, resetOptions.IncludeAbortedTests);

            // Clear selection after reset
            await _phDataGrid.ClearSelectedItem();
            _selectedItems!.Clear();
        }
    }

    private void UpdateButtonTexts()
    {
        var selectionCount = SelectionCount();
        if (selectionCount > 0)
        {
            // Use singular for 1 item, plural for multiple items
            TextPrimaryButton = string.Format(selectionCount == 1
                ? Loc["RequestListPage_DeleteDialogButton_Single"]
                : Loc["RequestListPage_DeleteDialogButton_Multiple"], selectionCount);
        }
    }

    private static string GetRequestDetailsTubesHref(int requestId)
    {
        return $"{PageRoute.SampleDetails.Default}/{requestId}?{RequestDetailPage.TabQueryParameterName}={RequestDetailPage.TubesTabName}";
    }

    private static StatusType GetStatusType(RequestState state)
    {
        return state switch
        {
            RequestState.NotStarted => StatusType.WorkflowNotStarted,
            RequestState.Started => StatusType.WorkflowInProgress,
            RequestState.PartlyCompleted => StatusType.WorkflowInProgress,
            RequestState.InReview => StatusType.WorkflowInProgress,
            RequestState.Completed => StatusType.WorkflowCompleted,
            _ => StatusType.None
        };
    }
    
    private static StatusType GetIssueStatusType(RequestIssueUrgency urgency)
    {
        return urgency switch
        {
            RequestIssueUrgency.Information => StatusType.Info,
            RequestIssueUrgency.Warning => StatusType.Warning,
            _ => throw new ArgumentOutOfRangeException(nameof(urgency), urgency, null)
        };
    }

    private string GetIssueText(RequestIssue issue)
    {
        if(issue.IssueType == RequestIssueType.TestsRemainingLoadTube)
        {
            return Loc["RequestIssueType_TestsRemainingLoadTube_WithKey", issue.Key];
        }
        if (issue.IssueType == RequestIssueType.TestsRemainingMoveTube)
        {
            //split key
            var instruments = issue.Key?.Split(Convert.ToChar(31)) ?? [];
            if(instruments.Length == 2)
            {
                return Loc["RequestIssueType_TestsRemainingMoveTube_WithKey", instruments];
            }
        }

        return issue.IssueType.ToLocalizedString();
    }
}
