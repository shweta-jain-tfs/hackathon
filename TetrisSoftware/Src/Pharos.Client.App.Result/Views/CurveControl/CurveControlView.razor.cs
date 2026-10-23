using System.ComponentModel;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.Result.Views.CalibratorCurve;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.State.CurrentInstrument;
using Pharos.Client.Common.ViewModels.Interfaces;
using Pharos.Common.Web.Components.Layout;
using Pharos.Common.Web.Components.Modal;
using Pharos.Contracts;
using Pharos.Contracts.Security;


namespace Pharos.Client.App.Result.Views.CurveControl;

public partial class CurveControlView(IAuthorizationService authorizationService, IClientDeviceService clientDeviceService) : IDisposable
{
    [Inject] private CurveControlChartViewModel ViewModel { get; set; } = default!;
    [Inject] private ICurrentInstrumentViewModel CurrentInstrumentViewModel { get; set; } = default!;
    [Inject] private CurveViewModel CurveViewModel { get; set; } = default!;

    [Inject] public IModalService ModalService { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private IExceptionHandlerService ExceptionHandlerService { get; set; } = default!;
    
    [Parameter] public ColumnSpan ColumnSpan { get; set; } = ColumnSpan.Half;
    [Parameter] public int AnalyticalRunId { get; set; }
    [Parameter] public string MethodName { get; set; } = string.Empty;
    [Parameter] public string InstrumentSerial { get; set; } = string.Empty;


    private bool SingleChart => ColumnSpan == ColumnSpan.Full;

    private bool ShowAcceptCurve { get; set; }
    private bool ShowSelectCurve { get; set; }
    private bool ShowRunNewCurve { get; set; }
    private bool ShowRunNewCurveControl { get; set; }
    private bool IsRndMode { get; set; }
    private bool HasApproveResultPermission { get; set; }


    protected override async Task OnInitializedAsync()
    {
        ViewModel.AnalyticalRunId = AnalyticalRunId;
        CurveViewModel.AnalyticalRunId = AnalyticalRunId;
        ViewModel.MethodName = MethodName;
        ViewModel.InstrumentSerial = InstrumentSerial;
        ViewModel.Miniature = false;
        await ViewModel.LoadAsync();
        ColumnSpan = ViewModel.ChartHolders.Count > 1 ? ColumnSpan.Half : ColumnSpan.Full;

        HasApproveResultPermission = await authorizationService.IsAuthorized(Permissions.ApproveTestResults);
        IsRndMode = await authorizationService.IsRndMode();


        SetButtonAccess();
        ViewModel.PropertyChanged += OnPropertyChanged;
    }

    private void SetButtonAccess()
    {
        ShowAcceptCurve = false;
        ShowSelectCurve = false;
        ShowRunNewCurve = false;
        ShowRunNewCurveControl = false;
        if (!HasApproveResultPermission)
        {
            return;
        }

        var chartHolders = ViewModel.ChartHolders;
        var chart = chartHolders.Count > 0 ? chartHolders[0] : null;
        var judgement = chart?.CurveControlTotalJudgment;
        var isFlagged = judgement is CurveControlJudgmentResult.Flagged;
        var isAccepted = isFlagged && chart?.FrozenAt != null;

        if (isAccepted)
        {
            ShowAcceptCurve = false;
            ShowSelectCurve = IsRndMode;
            ShowRunNewCurve = false;
            ShowRunNewCurveControl = false;

        }
        else
        {
            // If curve control is OK, only RndUser can select curve
            if (judgement is CurveControlJudgmentResult.OK)
            {
                ShowSelectCurve = IsRndMode;
                return;
            }

            // For non-OK judgments, always allow curve selection
            ShowSelectCurve = true;


            if (isFlagged)
            {
                ShowAcceptCurve = isFlagged;
            }

            // Allow rerun if client is instrument and if chart instrument is in assay
            if (clientDeviceService.ClientDeviceType == ClientDeviceType.Instrument &&
                CurrentInstrumentViewModel.State == InstrumentState.Assay &&
                CurrentInstrumentViewModel.SerialNumber == InstrumentSerial)
            {
                if (isFlagged)
                {
                    ShowRunNewCurveControl = true;
                }
                ShowRunNewCurve = true;
            }
        }
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.ChartHolders))
        {
            SetButtonAccess();
            StateHasChanged();
        }
    }

    public void Dispose()
    {
        ViewModel.PropertyChanged -= OnPropertyChanged;
        ViewModel.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task OpenRerunCurveControlModal()
    {
        var modal = ModalService.Show<RerunCurveControlModalView>(new ModalOptions
        {
            Title = "Rerun curve control",
            ModalHeight = ModalSize.Small,
            ModalWidth = ModalSize.Small
        });

        var result = await modal.Result;
        if (!result.Canceled)
        {
            await ViewModel.RerunCurveControl();
        }
    }

    private async Task ReRunCurveOpen()
    {
        var modal = ModalService.Show<ReRunModalView>(new ModalOptions
        {
            Title = "Rerun curve",
            ModalHeight = ModalSize.Small,
            ModalWidth = ModalSize.Small
        });

        var result = await modal.Result;
        if (!result.Canceled)
        {
            await CurveViewModel.ReRunCurve();
        }
    }

    private async Task SelectCurveOpen()
    {
        await CurveViewModel.OpenSelectCurveModal(ModalService, Loc);
    }

    private async Task AcceptCurveModal()
    {
        var modal = ModalService.Show<AcceptCurveModalView>(new ModalOptions
        {
            Title = Loc["AcceptCurveControlModalView_Title"],
            ModalHeight = ModalSize.Medium,
            ModalWidth = ModalSize.Medium,
        });

        var result = await modal.Result;
        if (!result.Canceled)
        {
            await CurveViewModel.AcceptCurve();
        }
    }

    private static StatusType GetStatusSymbol(ChartHolder chart)
    {
        return chart.CurveControlTotalJudgment switch
        {
            CurveControlJudgmentResult.NotJudged => StatusType.None,
            CurveControlJudgmentResult.Flagged => StatusType.Warning,
            CurveControlJudgmentResult.NotOK or CurveControlJudgmentResult.ReRunNotOK => StatusType.Alarm,
            _ => chart.Status
        };
    }

    private async Task OpenDetailsView(CurveControlTestResult context)
    {
        var parameters = new ModalParameters
        {
            { nameof(CurveControlDetailsModalView.CanEdit), HasApproveResultPermission},
            { nameof(CurveControlDetailsModalView.TestResult), context}
        };

        var modalOptions = new ModalOptions
        {
            ModalHeight = ModalSize.Large,
            ModalWidth = ModalSize.Large,
            Title = Loc["CurveControlDetailsModalView_Title"]
        };

        var modal = ModalService.Show<CurveControlDetailsModalView>(modalOptions, parameters);
        var result = await modal.Result;

        if (result.Canceled || result.Data == null)
        {
            return;
        }

        try
        {
            await ViewModel.SaveAsync((result.Data as CurveControlTestResult)!);

            StateHasChanged();
        }
        catch (Exception ex)
        {
            // Keep page interactive on unexpected error in this action.
            ExceptionHandlerService.ShowExceptionMessage(ex);
        }
    }

    private static string GetConcentrationText(CurveControlTestResult context, string concentration)
    {
        var conc = concentration.FormatDecimalForDisplay();

        if (string.IsNullOrWhiteSpace(conc))
        {
            return string.Empty;
        }

        return $"{conc} {context.Unit}";
    }

    private static string GetConcentrationText(CurveControlTestResult context, double? expectedConcentration)
    {
        if (expectedConcentration.HasValue)
        {
            return GetConcentrationText(context, expectedConcentration.Value.ToString());
        }

        return string.Empty;
    }
}