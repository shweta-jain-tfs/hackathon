using System.Collections.Specialized;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.InstrumentManagement.ViewModels.Dashboard;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.State.CurrentInstrument;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using Pharos.Common.Web.Components.Modal;
using Pharos.Contracts.Security;

namespace Pharos.Client.App.InstrumentManagement.Views.Dashboard;

public partial class MethodSettingsView : IDisposable
{
    private bool _canManageInstrument;
    [Inject] private ICurrentInstrumentViewModel InstrumentModel { get; set; } = null!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = null!;
    [Inject] private IModalService Modal { get; set; } = null!;
    [Inject] private MethodSettingsViewModel ViewModel { get; set; } = null!;
    [Inject] public IAuthorizationService AuthorizationService { get; set; } = null!;

    [Parameter] public string? MethodNameFilter { get; set; }
    [Parameter] public bool ShowActiveSwitch { get; set; } = true;
    [Parameter] public bool ShowRunColumns { get; set; }
    [Parameter] public required string TableHeader { get; set; }

    protected override async Task OnInitializedAsync()
    {
        ViewModel.CollectionChanged += OnItemCollectionChanged;
        await ViewModel.LoadAsync();
        _canManageInstrument = await AuthorizationService.IsAuthorized(Permissions.ManageInstruments);
    }

    private void OnItemCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        StateHasChanged();
    }

    private IQueryable<InstrumentMethod> FilteredInstrumentMethodList => string.IsNullOrWhiteSpace(MethodNameFilter)
        ? ViewModel.AsQueryable()
        : ViewModel.AsQueryable().Where(x => x.MethodName.Contains(MethodNameFilter, StringComparison.OrdinalIgnoreCase));

    private async Task ChangeActive(InstrumentMethod instrumentMethod)
    {
        if (instrumentMethod.IsActive &&
            InstrumentModel.State is
                InstrumentState.Assay or
                InstrumentState.AssayFinishOrdered or
                InstrumentState.BlankMeasurement or
                InstrumentState.Priming
           )
        {
            // Set back the switch button temporary.
            instrumentMethod.IsActive = false;

            var modal = Modal.Show<ActivateMethodModalView>(
                new ModalOptions
                {
                    Title = string.Format(Loc["DashboardPage_ActivateMethodModal_Title"], instrumentMethod.MethodName),
                    ModalHeight = ModalSize.Small,
                    ModalWidth = ModalSize.Small
                },
                new ModalParameters
                {
                    {
                        nameof(ActivateMethodModalView.MethodNameFilter), instrumentMethod.MethodName
                    },
                });

            var result = await modal.Result;

            if (!result.Canceled)
            {
                // Get fresh data from ViewModel to ensure we have the latest WhenToRun and other properties
                var freshMethod = ViewModel.FirstOrDefault(m => m.Id == instrumentMethod.Id);
                if (freshMethod != null)
                {
                    // Update the IsActive flag on the fresh data
                    freshMethod.IsActive = true;
                    await ViewModel.ChangeActive(freshMethod);
                }
                else
                {
                    // Fallback: use the original method if we can't find it
                    instrumentMethod.IsActive = true;
                    await ViewModel.ChangeActive(instrumentMethod);
                }
            }
        }
        else
        {
            await ViewModel.ChangeActive(instrumentMethod);
        }
    }

    public async Task ChangeRunOptions(InstrumentMethod instrumentMethod)
    {
        var copiedViewModel = new AssaySelectRunViewModel(new InstrumentMethod
        {
            Id = instrumentMethod.Id,
            CalibrationValidForDays = instrumentMethod.CalibrationValidForDays,
            IsActive = instrumentMethod.IsActive,
            MethodName = instrumentMethod.MethodName,
            Calibrator = instrumentMethod.Calibrator,
            ForceCalibrator = instrumentMethod.ForceCalibrator,
            WhenToRun = instrumentMethod.WhenToRun,
        });

        var modal = Modal.Show<AssaySelectRunModalView>(
            new ModalOptions
            {
                Title = Loc["DashboardPage_AssaySelectRunModal_Title"],
                ModalHeight = ModalSize.Large,
                ModalWidth = ModalSize.Medium
            },
            new ModalParameters
            {
                { nameof(AssaySelectRunModalView.ViewModel), copiedViewModel }
            });

        var result = await modal.Result;
        if (result is { Canceled: false, Data: AssaySelectRunViewModel updatedViewModel })
        {
            var method = new InstrumentMethod
            {
                Id = instrumentMethod.Id,
                IsActive = updatedViewModel.IsActive,
                MethodName = updatedViewModel.MethodName,
                Calibrator = updatedViewModel.Calibrator,
                ForceCalibrator = updatedViewModel.ForceCalibrator,
                WhenToRun = updatedViewModel.WhenToRun
            };

            await ViewModel.ChangeRunOptions(method);

            // Don't call LoadAsync here - the SignalR notification will trigger it automatically
            // This prevents concurrent updates exception
        }

        copiedViewModel.Dispose();
    }

    private string GetWhatToRunText(InstrumentMethod context)
    {
        // When Calibrator is true: always show "New calibration curve" (ForceCalibrator is irrelevant)
        // When Calibrator is false: show based on ForceCalibrator
        // - If ForceCalibrator is true: show "New calibration curve"
        // - If ForceCalibrator is false: show "Curve Controls"
        return context.Calibrator || context.ForceCalibrator
            ? Loc["DashboardPage_MethodSettingsView_Calibration"]
            : Loc["DashboardPage_MethodSettingsView_CurveControls"];
    }

    public void Dispose()
    {
        ViewModel.CollectionChanged -= OnItemCollectionChanged;
        ViewModel.Dispose();
        GC.SuppressFinalize(this);
    }
}