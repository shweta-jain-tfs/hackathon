using System.Globalization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.Result.Views.QualityControl;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.Services;
using Pharos.Client.Common.ViewModels.Interfaces;
using Pharos.Common.Web.Components.DataGrid;
using Pharos.Common.Web.Components.FormElements;
using Pharos.Common.Web.Components.Modal;
using Pharos.Common.Web.Components.Toaster;
using Pharos.Contracts.Security;

namespace Pharos.Client.App.Result.Views.Result;

public partial class TestResultsListView : IDisposable
{
    [Inject] private TestResultViewModel ViewModel { get; set; } = default!;
    [Inject] private IEntityNotificationProvider<AnalyticalRunTestResult> TestResultNotificationProvider { get; set; } = default!;
    [Inject] private IInstrumentDilutionService InstrumentDilutionService { get; set; } = default!;
    [Inject] private IModalService Modal { get; set; } = default!;
    [Inject] public IAuthorizationService AuthorizationService { get; set; } = null!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private IToasterService ToasterService { get; set; } = default!;
    [Inject] private UiSettingsService UiSettingsService { get; set; } = default!;
    [Inject] private IExceptionHandlerService ExceptionHandlerService { get; set; } = default!;

    private HashSet<AnalyticalRunTestResult>? _selectedSampleTestResults;

    [Parameter] public int AnalyticalRunId { get; set; }
    [Parameter] public Guid? TestOrderId { get; set; }
    [Parameter] public bool Expanded { get; set; }
    [Parameter] public bool CanExpand { get; set; } = true;
    [Parameter] public EventCallback<bool> OnResize { get; set; }
    [Parameter] public EventCallback<StatusType> StatusUpdated { get; set; }

    protected override void OnParametersSet()
    {
        if (TestOrderId.HasValue)
        {
            _stateId = null;
            _highlightOptions = new()
            {
                Mode = HighlightMode.ExternalState,
                IsHighlighted = x => x.TestOrderId == TestOrderId.Value
            };
        }
        else
        {
            _stateId = "testresults";
            _highlightOptions = null;
        }
    }

    private bool IsApproveDisable => SelectedApproveCount == 0;
    private bool IsRejectDisable => RejectCount == 0;
    private bool IsPendingReflexDisable => PendingReflexCount == 0;
    private bool HasApproveRejectPermission { get; set; }
    private bool IsServiceRole { get; set; }
    private bool IsRndMode { get; set; }
    private bool HasManualReflexDefinition { get; set; }
    public bool ShowReflexButton { get; set; }
    private bool IsResetEnabled => _selectedSampleTestResults?.Count(x => x.Status is TestResultStatus.Approved or TestResultStatus.Rejected or TestResultStatus.ApprovedPendingReflex) > 0;

    private HighlightOptions<AnalyticalRunTestResult>? _highlightOptions;

    private static readonly ItemStyleOptions<AnalyticalRunTestResult> _qcRowStyleOptions = new()
    {
        Items = [new object()],
        Match = (item, _) => item.ResultType == TestResultType.QualityControl,
        Style = ItemStyleFlags.CheckboxHidden
    };

    private PhDataGrid<AnalyticalRunTestResult>? _dataGrid;
    private StatusType _statusSummary = StatusType.None;
    private string? _stateId = "testresults";

    //todo: temporary solution for localized strings in quick filter
    private readonly Dictionary<string, string> _statusDict = new()
    {
        { TestResultStatus.Processing.ToString(), TestResultStatus.Processing.ToLocalizedString("TestResultDetails") },
        { TestResultStatus.PendingApproval.ToString(), TestResultStatus.PendingApproval.ToLocalizedString("TestResultDetails") },
        { TestResultStatus.Flagged.ToString(), TestResultStatus.Flagged.ToLocalizedString("TestResultDetails") },
        { TestResultStatus.Aborted.ToString(), TestResultStatus.Aborted.ToLocalizedString("TestResultDetails") },
        { TestResultStatus.Rejected.ToString(), TestResultStatus.Rejected.ToLocalizedString("TestResultDetails") },
        { TestResultStatus.Approved.ToString(), TestResultStatus.Approved.ToLocalizedString("TestResultDetails") },
        { TestResultStatus.Reported.ToString(), TestResultStatus.Reported.ToLocalizedString("TestResultDetails") },
        { TestResultStatus.ApprovedPendingReflex.ToString(), TestResultStatus.ApprovedPendingReflex.ToLocalizedString("TestResultDetails") }
    };

    private readonly Dictionary<string, string> _resultTypeDict = new()
    {
        { TestResultType.Sample.ToString(), TestResultType.Sample.ToLocalizedString("AnalyticalRunDetailsPage") },
        { TestResultType.QualityControl.ToString(), TestResultType.QualityControl.ToLocalizedString("AnalyticalRunDetailsPage") }
    };

    protected override async Task OnInitializedAsync()
    {
        HasApproveRejectPermission = await AuthorizationService.IsAuthorized(Permissions.ApproveTestResults);
        IsServiceRole = await AuthorizationService.IsServiceRole();
        IsRndMode = await AuthorizationService.IsRndMode();

        //Only check this once
        HasManualReflexDefinition = await SetHasManualReflexDefinition();
        if (HasManualReflexDefinition)
        {
            ShowReflexButton = true;
        }
        else
        {
            await SetShowReflex();
        }

        _statusSummary = await ViewModel.GetOverallStatusType(AnalyticalRunId);

        TestResultNotificationProvider.Created += OnSampleTestResultChanged;
        TestResultNotificationProvider.Updated += OnSampleTestResultChanged;
        TestResultNotificationProvider.Deleted += OnSampleTestResultChanged;
    }

    private Task OnSampleTestResultChanged(int id)
    {
        _ = InvokeAsync(async () =>
        {
            _statusSummary = await ViewModel.GetOverallStatusType(AnalyticalRunId);

            if (!HasManualReflexDefinition)
            {
                await SetShowReflex();
            }

            StateHasChanged();
        });

        return Task.CompletedTask;
    }

    private async Task<bool> SetHasManualReflexDefinition()
    {
        // Load UI settings to check if reflex testing is enabled
        var uiSettings = await UiSettingsService.GetSettingsAsync();
        var isReflexTestingEnabled = uiSettings.RequestSettings.ReflexTestingEnabled;

        //If reflex testing is enabled, check if there are any manual reflex definitions
        if (isReflexTestingEnabled)
        {
            return await ViewModel.HasManualReflexDefinitions();
        }

        return false;
    }

    private async Task SetShowReflex()
    {
        //Check if any test result in this run has status Pending reflex action
        ShowReflexButton = await ViewModel.GetTestResultsPendingReflex(AnalyticalRunId);

        if (!ShowReflexButton)
        {
            //Check if we have any manual reflex definitions for any results in this run
            ShowReflexButton = await ViewModel.HasManualReflexDefinitionsInRequestCopy(AnalyticalRunId);
        }
    }

    private async void OnViewRefreshNeeded(object? sender, EventArgs e)
    {
        _statusSummary = await ViewModel.GetOverallStatusType(AnalyticalRunId);

        if (!HasManualReflexDefinition)
        {
            await SetShowReflex();
        }

        await InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        TestResultNotificationProvider.Created -= OnSampleTestResultChanged;
        TestResultNotificationProvider.Updated -= OnSampleTestResultChanged;
        TestResultNotificationProvider.Deleted -= OnSampleTestResultChanged;

        ViewModel.Dispose();
        GC.SuppressFinalize(this);
    }

    private void SelectedItemsChanged(HashSet<AnalyticalRunTestResult> selectedItems)
    {
        _selectedSampleTestResults = selectedItems;
    }

    private async Task HandleResetResults()
    {
        var totalSelectedItems = _selectedSampleTestResults?.Count ?? 0;
        var testOrderIdsToReset = _selectedSampleTestResults?.Where(x => x.Status is TestResultStatus.Approved or TestResultStatus.Rejected or TestResultStatus.ApprovedPendingReflex && x.TestOrderId.HasValue)
            .Select(x => x.TestOrderId!.Value).ToList() ?? [];

        if (testOrderIdsToReset == null || testOrderIdsToReset.Count == 0)
        {
            return;
        }

        var testOrderIdsToResetCount = testOrderIdsToReset.Count;

        if (testOrderIdsToResetCount == 0)
        {
            return;
        }

        var modal = Modal.ShowConfirm(Loc["ResetTestsModal_ActionText", testOrderIdsToResetCount, totalSelectedItems], Loc["ResetTestsModal_Cancel"], Loc["ResetTestsModal_Ok", testOrderIdsToResetCount], new ModalOptions
        {
            Status = ModalStatus.Default,
            ModalHeight = ModalSize.XSmall,
            ModalWidth = ModalSize.Small,
            Title = Loc["ResetTestsModal_Title"]
        });

        var result = await modal.Result;
        if (result.Canceled)
        {
            return;
        }

        var resetCount = await ViewModel.ResetTestResults(testOrderIdsToReset);

        // Show toast
        var toasterText = Loc["ResetTests_Toast", resetCount];
        ToasterService.ShowToast(ToastType.Confirmation, toasterText);

        ClearSelections();
    }

    public async Task HandleRejectResults()
    {
        if (_selectedSampleTestResults is { Count: 1 })
        {
            await RejectSingleResult();
        }
        else
        {
            await RejectResults();
        }
    }

    private async Task HandleApproveResults()
    {
        var hasMixedFlaggedSelection = _selectedSampleTestResults is { Count: > 1 } && _selectedSampleTestResults.Any(x => x.Status == TestResultStatus.Flagged);

        var testResultsToApprove = _selectedSampleTestResults?.Where(x => x is
        {
            ApprovalStatus: ResultApprovalStatus.NotApproved,
            Status: not TestResultStatus.Aborted
                and not TestResultStatus.Processing
                and not TestResultStatus.PendingBlank
                and not TestResultStatus.PendingCalibration
        } && (!hasMixedFlaggedSelection || x.Status != TestResultStatus.Flagged) && x.TestOrderId.HasValue)
            .Select(t => t.TestOrderId!.Value)
            .ToList();

        if (testResultsToApprove is { Count: 0 } && !hasMixedFlaggedSelection)
        {
            return;
        }

        var parameters = new ModalParameters
        {
            { nameof(ApproveResultsModalView.TestsSelectedCount), _selectedSampleTestResults?.Count },
            { nameof(ApproveResultsModalView.TestToApproveCount), testResultsToApprove?.Count ?? 0 }
        };

        var modalOptions = new ModalOptions
        {
            ModalHeight = ModalSize.XSmall,
            ModalWidth = ModalSize.Small,
            Title = Loc["ApproveTestsModal_Title"]
        };

        var modal = Modal.Show<ApproveResultsModalView>(modalOptions, parameters);
        var result = await modal.Result;

        if (result.Canceled)
        {
            return;
        }

        var approvedCount = await ViewModel.ApproveTestResultsAsync(testResultsToApprove!);
        // Show toaster when request is saved.
        var toasterText = string.Format(Loc["ApproveTests_Toast"], approvedCount);
        ToasterService.ShowToast(ToastType.Confirmation, toasterText);

        ClearSelections();
    }

    private async Task OpenApproveRejectTestDialog(AnalyticalRunTestResult testResult)
    {
        if (!CanApproveOrRejectFromStatusLink(testResult) || !testResult.TestOrderId.HasValue)
        {
            return;
        }

        var parameters = new ModalParameters
        {
            { nameof(ApproveRejectTestModalView.SampleIdDisplay), testResult.SampleIdDisplay },
            { nameof(ApproveRejectTestModalView.TestName), testResult.TestName }
        };

        var modalOptions = new ModalOptions
        {
            ModalHeight = ModalSize.XSmall,
            ModalWidth = ModalSize.Small,
            Title = Loc["ApproveRejectTestModal_Title"]
        };

        var modal = Modal.Show<ApproveRejectTestModalView>(modalOptions, parameters);
        var result = await modal.Result;

        if (result.Canceled || result.Data is not ApproveRejectTestAction action)
        {
            return;
        }

        if (action == ApproveRejectTestAction.Approve)
        {
            var approvedCount = await ViewModel.ApproveTestResultsAsync([testResult.TestOrderId.Value]);
            var toasterText = string.Format(Loc["ApproveTests_Toast"], approvedCount);
            ToasterService.ShowToast(ToastType.Confirmation, toasterText);
            ClearSelections();
        }
        else
        {
            // Follow the same confirmation flow as the page-level Reject button before rejecting.
            await RejectSingleTestResult(testResult);
        }
    }

    private async Task RejectResults()
    {
        var testIdsToReject = _selectedSampleTestResults?.Where(x => x.ApprovalStatus == ResultApprovalStatus.NotApproved && x.TestOrderId.HasValue)
            .Select(x => x.TestOrderId!.Value).ToList();

        if (testIdsToReject is { Count: 0 })
        {
            return;
        }

        var parameters = new ModalParameters
        {
            { nameof(RejectResultsModalView.TestsSelectedCount), _selectedSampleTestResults?.Count},
            { nameof(RejectResultsModalView.TestsToRejectCount), testIdsToReject?.Count}
        };

        var modalOptions = new ModalOptions
        {
            ModalHeight = ModalSize.Small,
            ModalWidth = ModalSize.Small,
            Title = Loc["RejectTestsModal_TitleMultiple"]
        };

        var modal = Modal.Show<RejectResultsModalView>(modalOptions, parameters);
        var result = await modal.Result;

        if (result.Canceled)
        {
            return;
        }

        var addNewTests = false;
        if (result.Data is bool parsedValue)
        {
            addNewTests = parsedValue;
        }

        var rejectedCount = await ViewModel.RejectTestResultsAsync(testIdsToReject!, addNewTests);

        if (addNewTests && rejectedCount > 0)
        {
            // Show toast when rejected request is posted with copy test option.
            var toasterText = string.Format(Loc["RejectTests_ToastCopyTest"], rejectedCount);
            ToasterService.ShowToast(ToastType.Information, toasterText);
        }
        else
        {
            // Show toast when rejected request is posted
            var toasterText = string.Format(Loc["RejectTests_Toast"], rejectedCount);
            ToasterService.ShowToast(ToastType.Information, toasterText);
        }

        ClearSelections();
    }

    private async Task AddPendingReflexTests()
    {
        var pendingReflexResults = _selectedSampleTestResults?.Where(x => x.Status == TestResultStatus.ApprovedPendingReflex)
            .ToList();

        if (pendingReflexResults is { Count: 0 })
        {
            return;
        }

        // Get info for modal
        var testsPendingReflex = await ViewModel.GetPendingReflexes(pendingReflexResults!.Where(x => x.TestOrderId.HasValue).Select(x => x.TestOrderId!.Value).ToList());

        // Enhance PendingReflex objects with result information from TestResultWithReflex
        EnhancePendingReflexWithResultInfo(testsPendingReflex, pendingReflexResults!);

        var parameters = new ModalParameters
        {
            { nameof(PendingReflexModalView.TestsSelected), testsPendingReflex }
        };

        var modalOptions = new ModalOptions
        {
            ModalHeight = ModalSize.Large,
            ModalWidth = ModalSize.Medium,
            Title = Loc["PendingReflexModalView_Title"]
        };

        var modal = Modal.Show<PendingReflexModalView>(modalOptions, parameters);
        var result = await modal.Result;

        if (result.Canceled)
        {
            return;
        }

        ClearSelections();
    }

    private void ClearSelections()
    {
        _selectedSampleTestResults?.Clear();
        _dataGrid?.ClearSelectedItem();
    }

    private static void EnhancePendingReflexWithResultInfo(ICollection<PendingReflex> pendingReflexes, List<AnalyticalRunTestResult> testResultsWithReflex)
    {
        // Create a lookup dictionary by TestOrderId for efficient matching
        var testResultLookup = testResultsWithReflex
            .Where(tr => tr.TestOrderId.HasValue)
            .ToDictionary(tr => tr.TestOrderId!.Value, tr => tr);

        foreach (var pendingReflex in pendingReflexes)
        {
            if (testResultLookup.TryGetValue(pendingReflex.TestOrderId, out var testResultWithReflex))
            {
                // Populate the ResultValue from the TestResultWithReflex based on ResultType
                pendingReflex.ResultValue = GetResultValueFromTestResult(testResultWithReflex, pendingReflex.ResultType);
                // Note: TestResultWithReflex doesn't have Unit property, so we'll leave ResultUnit as is
                // or it will be populated later by the PendingReflexViewModel
            }
        }
    }

    private static string? GetResultValueFromTestResult(AnalyticalRunTestResult testResult, ResultType? resultType)
    {
        return resultType switch
        {
            ResultType.Concentration => testResult.MeanConcentrationAsText,
            ResultType.ConcentrationRaw => testResult.MeanConcentrationRaw,
            ResultType.Class => testResult.Class,
            ResultType.CutOff => testResult.CutOff,
            ResultType.Quotient => testResult.MeanQuotientAsText,
            ResultType.RU => testResult.MeanResponseAsText,
            _ => testResult.MeanConcentrationAsText // Fallback to concentration
        };
    }

    public async Task RejectSingleResult()
    {
        var testToReject = _selectedSampleTestResults?.FirstOrDefault();
        if (testToReject is not { ApprovalStatus: ResultApprovalStatus.NotApproved, TestOrderId: not null })
        {
            testToReject = null;
        }

        await RejectSingleTestResult(testToReject);
    }

    /// <summary>
    /// Opens the reject confirmation modal (with retest/dilution options) for a single test result
    /// and, if the user confirms, performs the rejection. Shared by the page-level Reject button
    /// and the Approve/Reject status link dialog so both follow the same confirmation flow.
    /// </summary>
    /// <param name="testToReject">The test result to reject, or null when nothing valid is selected.</param>
    private async Task RejectSingleTestResult(AnalyticalRunTestResult? testToReject)
    {
        var parameters = new ModalParameters
        {
            { nameof(RejectSingleResultModalView.TestToReject), testToReject}
        };

        //check null
        if (testToReject != null)
        {
            var factors = await InstrumentDilutionService.GetDilutionFactors(testToReject.MethodName, testToReject.TestName, testToReject.PreDilution);
            var factorOptions = factors.Where(x => x.IsValid).Select(x => new DropDownOption<int>
            {
                Value = x.DilutionFactor,
                Text = x.DilutionFactor.ToString(CultureInfo.InvariantCulture)
            }).ToList();

            parameters.Add(nameof(RejectSingleResultModalView.ValidDilutions), factorOptions);
            parameters.Add(nameof(RejectSingleResultModalView.OriginalInstrumentDilution), testToReject.InstrumentDilution);
            parameters.Add(nameof(RejectSingleResultModalView.OriginalPreDilution), testToReject.PreDilution);

        }
        var modalOptions = new ModalOptions
        {
            ModalHeight = testToReject == null ? ModalSize.XSmall : ModalSize.XLarge,
            ModalWidth = ModalSize.Small,
            Title = Loc["RejectSingleTestModal_Title"]
        };

        var modal = Modal.Show<RejectSingleResultModalView>(modalOptions, parameters);

        var result = await modal.Result;

        if (result.Canceled || testToReject == null)
        {
            return;
        }

        var retestOptions = result.Data as (DilutionOption? option, int newInstrumentDilution)? ?? (null, 0);
        var retest = retestOptions.option != null;

        var rejectedCount = await ViewModel.RejectTestResultsAsync(
            [testToReject.TestOrderId!.Value],
            retest,
            retestOptions.option == DilutionOption.ChangeInstrumentDilution ? retestOptions.newInstrumentDilution : null);

        if (rejectedCount > 0 && retest)
        {
            if (retestOptions.option == DilutionOption.SameDilutions)
            {
                // Show toast when rejected test with a new copy
                var toasterText = string.Format(Loc["RejectTests_ToastCopyTest"], 1);
                ToasterService.ShowToast(ToastType.Information, toasterText);
            }
            else
            {
                // Show toast when rejected test with a new copy and new dilution
                var toasterText = string.Format(Loc["RejectTests_ToastCopyTestWithNewDilution"], rejectedCount);
                ToasterService.ShowToast(ToastType.Information, toasterText);
            }
        }
        else
        {
            var toasterText = string.Format(Loc["RejectTests_Toast"], rejectedCount);
            ToasterService.ShowToast(ToastType.Information, toasterText);
        }

        ClearSelections();
    }
    private int SelectedApproveCount => _selectedSampleTestResults
        ?.Count(x => x is
        {
            ApprovalStatus: ResultApprovalStatus.NotApproved, Status: not TestResultStatus.Aborted
            and not TestResultStatus.Processing
            and not TestResultStatus.PendingBlank
            and not TestResultStatus.PendingCalibration
        }) ?? 0;

    private int RejectCount =>
        _selectedSampleTestResults
            ?.Count(x => x is
            {
                ApprovalStatus: ResultApprovalStatus.NotApproved
            }) ?? 0;

    private int PendingReflexCount => _selectedSampleTestResults
        ?.Count(x => x is
        {
            Status: TestResultStatus.ApprovedPendingReflex
        }) ?? 0;

    private bool CanApproveOrRejectFromStatusLink(AnalyticalRunTestResult testResult)
    {
        return HasApproveRejectPermission &&
               testResult is
               {
                   ApprovalStatus: ResultApprovalStatus.NotApproved,
                   Status: TestResultStatus.PendingApproval
               };
    }

    private EventCallback<MouseEventArgs> GetStatusClickCallback(AnalyticalRunTestResult testResult)
    {
        return CanApproveOrRejectFromStatusLink(testResult)
            ? EventCallback.Factory.Create<MouseEventArgs>(this, () => OpenApproveRejectTestDialog(testResult))
            : default;
    }

    private static StatusType GetStatusSymbol(TestResultStatus status)
    {
        return status switch
        {
            TestResultStatus.Aborted or TestResultStatus.Flagged => StatusType.Warning,
            TestResultStatus.PendingApproval or TestResultStatus.ApprovedPendingReflex => StatusType.Info,
            TestResultStatus.Processing or TestResultStatus.PendingBlank
                or TestResultStatus.PendingCalibration => StatusType.Processing,
            _ => StatusType.None
        };
    }

    private static string GetConcentrationText(AnalyticalRunTestResult context)
    {
        var conc = context.MeanConcentrationAsText.FormatDecimalForDisplay();

        if (string.IsNullOrWhiteSpace(conc))
        {
            return string.Empty;
        }

        return $"{conc} {context.Unit}";
    }

    private Task OpenDetailsView(AnalyticalRunTestResult testResult)
    {
        return testResult.ResultType switch
        {
            TestResultType.Sample => OpenSampleDetailsView(testResult.Id),
            TestResultType.QualityControl => OpenQualityControlDetailsView(testResult),
            _ => Task.CompletedTask
        };
    }

    private async Task OpenSampleDetailsView(int testResultId)
    {
        var sampleTestResult = await ViewModel.GetSampleTestResult(testResultId);

        var parameters = new ModalParameters
        {
            {
                nameof(SampleTestResultDetailsModalView.CanEdit),
                HasApproveRejectPermission
            },
            {
                nameof(SampleTestResultDetailsModalView.TestResult),
                sampleTestResult
            }
        };

        var modalOptions = new ModalOptions
        {
            ModalHeight = ModalSize.Large,
            ModalWidth = ModalSize.Large,
            Title = Loc["SampleTestResultDetailsModalView_Title"]
        };

        var modal = Modal.Show<SampleTestResultDetailsModalView>(
            modalOptions,
            parameters);

        var result = await modal.Result;

        if (result.Canceled || result.Data == null)
        {
            return;
        }

        try
        {
            await ViewModel.UpdateSampleTestResult(
                (SampleTestResult)result.Data);

            StateHasChanged();
        }
        catch (Exception ex)
        {
            ExceptionHandlerService.ShowExceptionMessage(ex);
        }
    }

    private async Task OpenQualityControlDetailsView(AnalyticalRunTestResult testResult)
    {
        var qualityControlTestResult = await ViewModel.GetQualityControlTestResult(testResult.Id);

        var parameters = new ModalParameters
        {
            {
                nameof(QualityControlTestResultDetailsModalView.CanEdit),
                HasApproveRejectPermission
            },
            {
                nameof(QualityControlTestResultDetailsModalView.TestResult),
                qualityControlTestResult
            }
        };

        var modalOptions = new ModalOptions
        {
            ModalHeight = ModalSize.Large,
            ModalWidth = ModalSize.Large,
            Title = Loc["QualityControlTestResultDetailsModalView_Title"]
        };

        var modal = Modal.Show<QualityControlTestResultDetailsModalView>(
            modalOptions,
            parameters);

        var result = await modal.Result;

        if (result.Canceled ||
            result.Data is not QualityControlTestResult updatedResult)
        {
            return;
        }

        try
        {
            await ViewModel.UpdateQualityControlAsync(
                updatedResult);

            testResult.Comment = updatedResult.Comment;

            StateHasChanged();
        }
        catch (Exception ex)
        {
            ExceptionHandlerService.ShowExceptionMessage(ex);
        }
    }
}
