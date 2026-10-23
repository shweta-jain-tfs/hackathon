using System.ComponentModel;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.Result.Views.CalibratorCurve;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.State.CurrentInstrument;
using Pharos.Client.Common.ViewModels.Interfaces;
using Pharos.Common.Web.Components.Modal;
using Pharos.Contracts;
using Pharos.Contracts.Security;

namespace Pharos.Client.App.Result.Views;

public partial class CalibratorPage
{
    [Parameter] public int Id { get; set; }
    [Inject] private MethodNameViewModel MethodNameViewModel { get; set; } = default!;
    [Inject] private CurveViewModel ViewModel { get; set; } = default!;
    [Inject] private ICurrentInstrumentViewModel CurrentInstrumentViewModel { get; set; } = default!;
    [Inject] public IModalService ModalService { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private IExceptionHandlerService ExceptionHandlerService { get; set; } = default!;

    private readonly IAuthorizationService _authorizationService;
    private readonly IClientDeviceService _clientDeviceService;

    public CalibratorPage(IAuthorizationService authorizationService, IClientDeviceService clientDeviceService)
    {
        _authorizationService = authorizationService;
        _clientDeviceService = clientDeviceService;
    }

    private string? MethodName { get; set; }
    private string ValidDateText { get; set; } = string.Empty;
    private StatusType ValidStatus { get; set; } = StatusType.Alarm;
    private bool IsRndMode { get; set; }
    private bool HasApproveResultPermission { get; set; }
    protected override async Task OnInitializedAsync()
    {
        var info = await MethodNameViewModel.GetMethodNameForRun(Id);
        MethodName = info.MethodName;
        ViewModel.AnalyticalRunId = Id;
        ViewModel.MethodName = MethodName;
        ViewModel.InstrumentSerial = info.InstrumentNumber;

        await ViewModel.LoadAsync();
        ViewModel.IsLoaded = true;
        SetValidationHeader();
        HasApproveResultPermission = await _authorizationService.IsAuthorized(Permissions.ApproveTestResults);
        IsRndMode = await _authorizationService.IsRndMode();

        SetButtonAccess();

        ViewModel.PropertyChanged += OnPropertyChanged;
    }

    private void SetButtonAccess()
    {
        ShowSelectCurveButton = false;
        ShowReRunCurveButton = false;
        ShowAcceptCurveButton = false;
        if (!HasApproveResultPermission)
        {
            return;
        }

        // RnD user can always access select curve button
        ShowSelectCurveButton = IsRndMode;

        var data = ViewModel.Data;

        if (data is null || !ViewModel.HasCurveDetails)
        {
            return;
        }

        var judgment = data.Judgment;
        var isAccepted = data.IsAccepted;

        if (isAccepted)
        {
            ShowSelectCurveButton = IsRndMode;
            return;
        }

        if (judgment is not CurveJudgment.OK)
        {
            ShowSelectCurveButton = true;

            if (_clientDeviceService.ClientDeviceType is ClientDeviceType.Instrument &&
                CurrentInstrumentViewModel.State is InstrumentState.Assay &&
                CurrentInstrumentViewModel.SerialNumber == ViewModel.InstrumentSerial)
            {
                ShowReRunCurveButton = true;
            }
            var curveDisabled = data.CurveControlDisabledFlag;
            if (judgment is CurveJudgment.Flagged && !curveDisabled)
            {
                ShowAcceptCurveButton = true;
            }
        }
    }

    protected List<BreadcrumbItem> Breadcrumbs =>
    [
        new BreadcrumbItem(Loc["CalibratorPage_BreadcrumbResults"], PageRoute.Results),
        new BreadcrumbItem(MethodName ?? "", $"{PageRoute.AnalyticalRunDetails.Default}/{Id}"),
        new BreadcrumbItem(Loc["CalibratorPage_BreadcrumbCalibrators"], "")
    ];

    public bool ShowSelectCurveButton { get; set; }
    public bool ShowReRunCurveButton { get; set; }
    public bool ShowAcceptCurveButton { get; set; }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        SetValidationHeader();
        SetButtonAccess();
        StateHasChanged();
    }

    private static StatusType GetStatus(CalibratorTestResult calibratorTestResult)
    {
        if (calibratorTestResult.Replicates.Any(replicate => replicate.IsAborted || replicate.HasError || replicate.IsLowResponse))
        {
            return StatusType.Alarm;
        }

        return calibratorTestResult.NormDoseCheckSummary switch
        {
            NormDoseCheckSummary.NotDefined => StatusType.None,
            NormDoseCheckSummary.OK => StatusType.None,
            NormDoseCheckSummary.SomeReplRemoved => StatusType.Warning,
            NormDoseCheckSummary.AllReplRemoved => StatusType.Warning,
            _ => throw new ArgumentOutOfRangeException(nameof(calibratorTestResult), calibratorTestResult.NormDoseCheckSummary, null)
        };
    }

    private void SetValidationHeader()
    {
        if (!ViewModel.HasCurveDetails)
        {
            return;
        }

        if (ViewModel.Data!.CurveControlDisabledFlag)
        {
            ValidStatus = StatusType.Disconnected;
            ValidDateText = Loc["CalibratorPage_DisabledByCurveControls"];
            return;
        }

        switch (ViewModel.Data.Judgment)
        {
            case CurveJudgment.NotOK:
                ValidStatus = StatusType.Alarm;
                break;
            case CurveJudgment.Flagged:
                ValidStatus = StatusType.Warning;
                break;
            case CurveJudgment.OK:
                ValidStatus = StatusType.Success;

                if (ViewModel.Data.CurveFittedFlag && ViewModel.Data.LastDayToUse != null)
                {
                    var lastDayToUse = ViewModel.Data.LastDayToUse.Value;
                    var daysLeft = lastDayToUse.DayNumber - DateOnly.FromDateTime(DateTimeOffset.Now.Date).DayNumber;
                    var expiryDate = lastDayToUse.ToShortDateString();

                    switch (daysLeft)
                    {
                        case < 0:
                            break;

                        case < 1:
                            ValidDateText = string.Format(Loc["CalibratorPage_ValidTextLessThanOneDay"], expiryDate);
                            break;

                        case < 2:
                            ValidDateText = string.Format(Loc["CalibratorPage_ValidTextOneDay"], expiryDate);
                            break;

                        default:
                            ValidDateText = string.Format(Loc["CalibratorPage_ValidText"], expiryDate, daysLeft);
                            break;
                    }
                }

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async Task DetailsViewOpen(CalibratorTestResult calibratorTestResult)
    {
        //save the original comment
        var comment = calibratorTestResult.Comment;
        var parameters = new ModalParameters
        {
            { nameof(CalibratorDetailsModalView.CanEdit), HasApproveResultPermission },
            { nameof(CalibratorDetailsModalView.CalibratorTestResult), calibratorTestResult }
        };

        var modal = ModalService.Show<CalibratorDetailsModalView>(new ModalOptions
        {
            Title = string.Format(Loc["CalibratorPage_CalibratorDetailsTitle"], calibratorTestResult.CalibratorPointName),
            ModalHeight = ModalSize.XLarge,
            ModalWidth = ModalSize.XLarge
        }, parameters);

        var x = await modal.Result;

        if (!x.Canceled)
        {
            try
            {
                //save the edited value
                await ViewModel.UpdateCalibratorResult(calibratorTestResult);
            }
            catch (Exception ex)
            {
                // Keep page interactive on unexpected error in this action.
                ExceptionHandlerService.ShowExceptionMessage(ex);
            }
        }
        else
        {
            //reset the comment, changes were discarded
            calibratorTestResult.Comment = comment;
        }
    }

    private async Task ReRunCurveOpen()
    {
        var modal = ModalService.Show<ReRunModalView>(new ModalOptions
        {
            Title = Loc["CalibratorPage_RerunCurveTitle"],
            ModalHeight = ModalSize.Small,
            ModalWidth = ModalSize.Small
        });

        var result = await modal.Result;
        if (!result.Canceled)
        {
            await ViewModel.ReRunCurve();
            StateHasChanged();
        }
    }

    public void Dispose()
    {
        ViewModel.PropertyChanged -= OnPropertyChanged;
        ViewModel.Dispose();
    }

    private async Task SelectCurveOpen()
    {
        var curveChanged = await ViewModel.OpenSelectCurveModal(ModalService, Loc);
        if (curveChanged)
        {
            StateHasChanged();
        }
    }

    private async Task AcceptCurveOpen()
    {
        var modal = ModalService.ShowConfirm(Loc["CalibratorPage_AcceptCurveText"], Loc["CalibratorPage_Cancel"], Loc["CalibratorPage_Ok"], new ModalOptions());

        var result = await modal.Result;

        if (!result.Canceled)
        {
            await ViewModel.AcceptCurve();
            await ViewModel.LoadAsync();
            SetButtonAccess();
            StateHasChanged();
        }
    }

    private static string GetConcentrationText(CalibratorTestResult context, string concentration)
    {
        var conc = concentration.FormatDecimalForDisplay();

        if (string.IsNullOrWhiteSpace(conc))
        {
            return string.Empty;
        }

        return $"{conc} {context.Unit}";
    }

    private static string GetConcentrationText(CalibratorTestResult context, double? expectedConcentration)
    {
        if (expectedConcentration.HasValue)
        {
            return GetConcentrationText(context, expectedConcentration.Value.ToString());
        }

        return string.Empty;
    }
}
