using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.Services;
using Pharos.Client.Common.ViewModels;
using Pharos.Client.Common.ViewModels.Interfaces;
using Pharos.Common.Web.Components.DataGrid;
using Pharos.Common.Web.Components.DataGrid.Filters.Models;
using Pharos.Common.Web.Components.Modal;
using Pharos.Common.Web.Components.QuickFilter;
using Pharos.Common.Web.Extensions;
using Pharos.Common.Web.Models.DataGrid;
using Pharos.Contracts.Security;

namespace Pharos.Client.App.Result.Views.Result;

public partial class AnalyticalRunListView
{
    private static readonly IReadOnlyList<IssueStatus> _noIssues = [];

    [Inject] private AnalyticalRunListViewModel ViewModel { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private InstrumentFilterViewModel InstrumentFilterModel { get; set; } = default!;
    [Inject] private IModalService ModalService { get; set; } = default!;
    [Inject] private UiSettingsService UiSettingsService { get; set; } = default!;
    [Inject] private IAuthorizationService AuthorizationService { get; set; } = default!;
    [Inject] private IExceptionHandlerService ExceptionHandlerService { get; set; } = default!;

    private HashSet<AnalyticalRun> _selectedAnalyticalRuns = [];
    private bool _isExporting;
    private IJSObjectReference? _script;
    private PhDataGrid<AnalyticalRun>? _dataGrid;
    private bool _canViewCustomRunId;

    private bool CanSelect { get; set; }
    private bool HasDeletePermission { get; set; }

    private const string _issueStatusFilterKey = "IssueStatus";

    private CustomQuickFilter[] _issuesQuickFilters = [];

    protected override async Task OnInitializedAsync()
    {
        var userSettings = await UiSettingsService.GetSettingsAsync();

        _issuesQuickFilters =
        [
            new(
                new MultiSelectQuickFilter<IssueStatus>(
                    _issueStatusFilterKey,
                    Loc["AnalyticalRunsPage_Issues"],
                    InitiateIssuesFilter(userSettings.RequestSettings.ReflexTestingEnabled),
                    status => status.ToLocalizedString("AnalyticalRunsPage")),
                typeof(PhQuickMultiSelectFilter<IssueStatus>))
        ];

        _canViewCustomRunId = await AuthorizationService.IsRndMode();
        var canExportTestResults = await AuthorizationService.IsAuthorized(Permissions.ExportTestResults);
        HasDeletePermission = await AuthorizationService.IsAuthorized(Permissions.ApproveTestResults);
        CanSelect = canExportTestResults || HasDeletePermission;
        await InstrumentFilterModel.LoadAsync();
        ViewModel.ViewRefreshNeeded += OnViewRefreshNeeded;
    }

    private static List<IssueStatus> InitiateIssuesFilter(bool reflexTestingEnabled)
    {
        var enums = Enum.GetValues<IssueStatus>().Where(x => x != IssueStatus.NoIssue).ToList();
        if (!reflexTestingEnabled)
        {
            enums.Remove(IssueStatus.ApprovedPendingReflex);
        }

        return enums.OrderBy(x => x.ToLocalizedString("AnalyticalRunsPage")).ToList();
    }

    private void OnViewRefreshNeeded(object? sender, EventArgs e)
    {
        InvokeAsync(StateHasChanged);
    }

    protected override async void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _script = await ImportCommonUtilityJSAsync("download.js");
        }
    }

    private bool CannotExport => _selectedAnalyticalRuns.Count == 0;

    private void SelectedAnalyticalRunsChanged(HashSet<AnalyticalRun> selectedItems)
    {
        _selectedAnalyticalRuns = selectedItems;
    }

    private async Task DownloadSelectedAnalyticalRuns()
    {
        if (_isExporting || _script is null || _selectedAnalyticalRuns.Count == 0)
        {
            return;
        }

        _isExporting = true;
        try
        {
            var file = await ViewModel.GetResultExportAsync(_selectedAnalyticalRuns.Select(x => x.Id).ToArray());

            var fileName = string.IsNullOrWhiteSpace(file.Name) ? "export.csv" : file.Name;

            using var streamRef = new DotNetStreamReference(stream: file.Stream);
            await _script.InvokeVoidAsync("downloadFromStreamReferenceWithDialog", fileName, streamRef);
        }
        catch (Exception ex)
        {
            // Keep page interactive on unexpected error in this action.
            ExceptionHandlerService.ShowExceptionMessage(ex);
        }
        finally
        {
            _isExporting = false;
        }
    }

    public override async ValueTask DisposeAsync()
    {
        await _script.SafeDisposeAsync();

        ViewModel.ViewRefreshNeeded -= OnViewRefreshNeeded;
        ViewModel.Dispose();

        GC.SuppressFinalize(this);
    }

    private static StatusType GetStatusSymbol(IssueStatus status)
    {
        return status switch
        {
            IssueStatus.CalibrationFailed or IssueStatus.CurveControlFailed => StatusType.Alarm,
            IssueStatus.CalibrationFlagged or IssueStatus.CurveControlFlagged => StatusType.Warning,
            IssueStatus.PendingApproval or IssueStatus.ApprovedPendingReflex or IssueStatus.QualityControlFailed or
                IssueStatus.QualityControlFlagged or IssueStatus.FailedTests => StatusType.Info,
            _ => StatusType.None
        };
    }

    private static string? GetPageRoute(IssueStatus issueStatus, int runId)
    {
        return issueStatus switch
        {
            IssueStatus.CurveControlFlagged or IssueStatus.CurveControlFailed => $"{PageRoute.CurveControlDetails.Default}/{runId}",
            IssueStatus.CalibrationFailed or IssueStatus.CalibrationFlagged => $"{PageRoute.CalibratorDetails.Default}/{runId}",
            IssueStatus.QualityControlFailed or IssueStatus.QualityControlFlagged => $"{PageRoute.QualityControlDetails.Default}/{runId}",
            IssueStatus.FailedTests or IssueStatus.PendingApproval or IssueStatus.ApprovedPendingReflex => $"{PageRoute.AnalyticalRunDetails.Default}/{runId}",
            _ => null
        };
    }

    private static IReadOnlyList<IssueStatus> GetIssues(AnalyticalRun run)
    {
        if (run.Issues is { Count: > 0 })
        {
            return run.Issues.ToList();
        }

        return run.IssueStatus == IssueStatus.NoIssue ? _noIssues : [run.IssueStatus];
    }

    private bool CanDelete => _selectedAnalyticalRuns.Count > 0;

    private async Task DeleteSelectedAnalyticalRuns()
    {
        var totalCount = _selectedAnalyticalRuns.Count;
        var stillProcessing = _selectedAnalyticalRuns.Count(x => x.StateStatus == StateStatus.Processing);
        var notReportedOrApproved = _selectedAnalyticalRuns.Count(x => x.StateStatus == StateStatus.Ready);

        // Confirmation dialog
        var parameters = new ModalParameters
        {
            {nameof(AnalyticalRunDeleteView.NumberOfRunsToBeDeleted), totalCount},
            {nameof(AnalyticalRunDeleteView.NumberToShow), stillProcessing + notReportedOrApproved}
        };

        var modal = ModalService.Show<AnalyticalRunDeleteView>(
            new ModalOptions
            {
                Title = Loc["AnalyticalRunDeleteView_Title"],
                ModalHeight = ModalSize.Small,
                ModalWidth = ModalSize.Small
            }, parameters);

        var result = await modal.Result;

        if (!result.Canceled)
        {
            try
            {
                await ViewModel.DeleteAnalyticalRuns(_selectedAnalyticalRuns.Select(x => x.Id));

                _selectedAnalyticalRuns.Clear();
                _dataGrid?.ClearSelectedItem();
            }
            catch (Exception ex)
            {
                // Keep page interactive on unexpected error in this action.
                ExceptionHandlerService.ShowExceptionMessage(ex);
            }
        }
    }
}