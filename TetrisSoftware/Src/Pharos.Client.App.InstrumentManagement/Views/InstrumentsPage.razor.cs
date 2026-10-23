using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.InstrumentManagement.Models.Instruments;
using Pharos.Client.App.InstrumentManagement.ViewModels.AssayScheduler;
using Pharos.Client.App.InstrumentManagement.ViewModels.Instruments;
using Pharos.Client.App.InstrumentManagement.ViewModels.TaskCards;
using Pharos.Client.App.InstrumentManagement.Views.AssayScheduler;
using Pharos.Client.App.InstrumentManagement.Views.Instruments;
using Pharos.Client.Common.App.Helpers;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.Services;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using Pharos.Common.Web.Components.FormElements;
using Pharos.Common.Web.Components.Health;
using Pharos.Common.Web.Components.InstrumentCard;
using Pharos.Common.Web.Components.Modal;
using Pharos.Contracts.Security;

namespace Pharos.Client.App.InstrumentManagement.Views;

public partial class InstrumentsPage : IDisposable
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] public IInstrumentNotificationProvider NotificationProvider { get; set; } = default!;
    [Inject] public IModalService ModalService { get; set; } = default!;
    [Inject] private TimerService TimerService { get; set; } = default!;
    [Inject] public IAuthorizationService AuthorizationService { get; set; } = null!;
    [Inject] private IInstrumentTechnologyDataService InstrumentTechnologyDataService { get; set; } = default!;
    [Inject] private InstrumentsViewModel InstrumentsViewModel { get; set; } = default!;
    [Inject] private TaskCardsListViewModel TaskCardsListViewModel { get; set; } = default!;

    private bool IsAuthorized { get; set; }

    private InstrumentSortOption _selectedSortOption = InstrumentSortOption.Name;
    private Task? _instrumentsLoadTask;

    private void InstrumentCardSelected(Instrument instrument)
    {
        // Deselect if the same card is tapped again; otherwise switch to the new card.
        // Only one card can be selected at a time.
        var newSerialNumber = instrument.SerialNumber == TaskCardsListViewModel.SelectedInstrumentCard
            ? null
            : instrument.SerialNumber;
        TaskCardsListViewModel.SelectInstrument(newSerialNumber);
    }

    private void OnTaskCardsViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        StateHasChanged();
    }

    private List<DropDownOption<InstrumentSortOption>> SortOptions => [
        new() { Value = InstrumentSortOption.Name, Text = Loc["InstrumentsPage_Sort_Name"] },
        new() { Value = InstrumentSortOption.Status, Text = Loc["InstrumentsPage_Sort_Status"] }
    ];

    private ICollection<Instrument> SortedInstruments => _selectedSortOption switch
    {
        InstrumentSortOption.Name => InstrumentsViewModel.OrderBy(i => i.DisplayName, StringComparer.CurrentCultureIgnoreCase).ToList(),
        InstrumentSortOption.Status => InstrumentsViewModel.OrderBy(i => i.State.ToLocalizedString(), StringComparer.CurrentCultureIgnoreCase).ToList(),
        _ => InstrumentsViewModel
    };

    /// <summary>
    /// Holds the value indicating whether any assay scheduler is active.
    /// </summary>
    private bool IsAnyAssaySchedulerActive =>
        InstrumentsViewModel.Any(instrument => instrument is { AssaySchedulerShowInfo: true, AssaySchedulerIsActive: true });

    private bool IsAnyAssaySchedulerShowInfo =>
        InstrumentsViewModel.Any(instrument => instrument.AssaySchedulerShowInfo);

    protected override async Task OnInitializedAsync()
    {
        IsAuthorized = await AuthorizationService.IsAuthorized(Permissions.ManageInstruments);

        NotificationProvider.AssaySchedulerChanged += OnAssaySchedulerChanged;
        NotificationProvider.AssaySchedulerDayChanged += OnAssaySchedulerDayChanged;
        NotificationProvider.AssaySchedulerNextRunChanged += OnAssaySchedulerNextRunChanged;
        NotificationProvider.InstrumentStateChanged += OnInstrumentStateChanged;
        NotificationProvider.TimeWhenNextStateReachedChanged += OnTimeWhenNextStateReachedChanged;
        NotificationProvider.InstrumentTechnologiesChanged += OnInstrumentTechnologiesChanged;
        NotificationProvider.NumberOfTestsUpdated += Refresh;
        NotificationProvider.SignalTowerStateChanged += Refresh;
        TimerService.OnTimerTick += OnTimerTick;
        InstrumentsViewModel.CollectionChanged += OnItemCollectionChanged;
        TaskCardsListViewModel.PropertyChanged += OnTaskCardsViewModelPropertyChanged;

        await ReloadInstrumentsAsync();
    }

    private void OnAssaySchedulerDayChanged(int instrumentId, int assaySchedulerId, AssaySchedulerDay assaySchedulerDay, bool anyDayHasTime)
    {
        var instrument = InstrumentsViewModel.FirstOrDefault(i => i.Id == instrumentId);
        if (instrument == null)
        {
            // TODO: Error handling?
            return;
        }

        instrument.AssaySchedulerShowInfo = anyDayHasTime;
    }

    private void OnAssaySchedulerChanged(int instrumentId, int assaySchedulerId, bool assaySchedulerIsActive, DateTimeOffset? assaySchedulerPausedUntilDate)
    {
        // Update assay scheduler information for the instrument.
        var instrument = InstrumentsViewModel.FirstOrDefault(i => i.Id == instrumentId);
        if (instrument == null)
        {
            // TODO: Error handling?
            return;
        }

        instrument.AssaySchedulerIsActive = assaySchedulerIsActive;
        instrument.AssaySchedulerIsPausedUntilDate = assaySchedulerPausedUntilDate;

        StateHasChanged();
    }

    private void OnAssaySchedulerNextRunChanged(int instrumentId, DateTimeOffset? nextRun, AssaySchedulerNextRunType nextRunType)
    {
        // Update assay scheduler information for the instrument.
        var instrument = InstrumentsViewModel.FirstOrDefault(i => i.Id == instrumentId);
        if (instrument == null)
        {
            // TODO: Error handling?
            return;
        }

        instrument.AssaySchedulerNextRunType = nextRunType;
        instrument.AssaySchedulerNextRun = nextRun;

        StateHasChanged();
    }

    private void OnInstrumentStateChanged(int instrumentId, InstrumentState instrumentState)
    {
        // Update the instrument state message.
        var instrument = InstrumentsViewModel.FirstOrDefault(i => i.Id == instrumentId);
        if (instrument == null)
        {
            // TODO: Error handling?
            return;
        }

        instrument.State = instrumentState;

        StateHasChanged();
    }

    private void OnTimeWhenNextStateReachedChanged(int instrumentId, DateTimeOffset? timeWhenNextStateReached)
    {
        var instrument = InstrumentsViewModel.FirstOrDefault(i => i.Id == instrumentId);
        if (instrument == null)
        {
            // TODO: Error handling?
            return;
        }

        instrument.TimeWhenNextStateReached = timeWhenNextStateReached;

        StateHasChanged();
    }

    private void OnInstrumentTechnologiesChanged(int instrumentId)
    {
        _ = InvokeAsync(ReloadInstrumentsAsync);
    }

    private void Refresh(object? sender, EventArgs e)
    {
        _ = InvokeAsync(ReloadInstrumentsAsync);
    }

    private void OnItemCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        StateHasChanged();
    }

    private void OnTimerTick()
    {
        StateHasChanged();
    }

    /// <summary>
    /// Handle the assay scheduler active/inactive switch for all instruments.
    /// </summary>
    /// <param name="isActive"></param>
    /// <returns></returns>
    private async Task OnAssaySchedulerIsActiveChangedAllInstruments(bool isActive)
    {
        if (isActive)
        {
            await InstrumentsViewModel.SetAllAssaySchedulersActive(true);
        }
        else
        {
            // Deactivating: show pause dialog.
            var result = await ShowPauseModal(AssaySchedulerPauseMode.AllInstruments, null);
            if (result is { Canceled: false, Data: AssaySchedulerPauseViewModel pauseViewModel })
            {
                foreach (var instrument in InstrumentsViewModel)
                {
                    instrument.AssaySchedulerIsActive = false;
                    instrument.AssaySchedulerIsPausedUntilDate = pauseViewModel.PausedUntilDate;
                    await InstrumentsViewModel.UpdateAssaySchedule(instrument);
                }
            }
        }
    }

    /// <summary>
    /// Handle the assay scheduler active/inactive switch for one instrument.
    /// </summary>
    /// <param name="instrument"></param>
    /// <param name="isActive"></param>
    /// <returns></returns>
    private async Task OnAssaySchedulerIsActiveChanged(Instrument instrument, bool isActive)
    {
        if (isActive)
        {
            instrument.AssaySchedulerIsActive = isActive;
            await InstrumentsViewModel.UpdateAssaySchedule(instrument);
        }
        else
        {
            // Set back the switch button temporary.
            instrument.AssaySchedulerIsActive = true;

            // Deactivating: show pause dialog.
            var result = await ShowPauseModal(AssaySchedulerPauseMode.CurrentInstrument, instrument);
            if (result is { Canceled: false, Data: AssaySchedulerPauseViewModel viewModel })
            {
                instrument.AssaySchedulerIsActive = false;
                instrument.AssaySchedulerIsPausedUntilDate = viewModel.PausedUntilDate;
                await InstrumentsViewModel.UpdateAssaySchedule(instrument);
            }
        }
    }

    /// <summary>
    /// Handle the pause dialog.
    /// </summary>
    /// <param name="mode"></param>
    /// <param name="instrument"></param>
    /// <returns></returns>
    private async Task<ModalResult> ShowPauseModal(AssaySchedulerPauseMode mode, Instrument? instrument)
    {
        var viewModel = new AssaySchedulerPauseViewModel
        {
            PausedUntilDate = null, // Always default with no date.
        };

        var modal = ModalService.Show<AssaySchedulerPauseModalView>(
            new ModalOptions
            {
                Title = Loc["AssaySchedulerPage_AssaySchedulerPauseModal_Title"],
                ModalHeight = ModalSize.Medium,
                ModalWidth = ModalSize.Small
            },
            new ModalParameters
            {
                { nameof(AssaySchedulerPauseModalView.InstrumentName), instrument?.DisplayName },
                { nameof(AssaySchedulerPauseModalView.Mode), mode },
                { nameof(AssaySchedulerPauseModalView.ViewModel), viewModel },
            });

        return await modal.Result;
    }

    private async Task OnDisableInstrument(Instrument instrument)
    {
        if (instrument.State is
            InstrumentState.Assay or
            InstrumentState.AssayCmr or
            InstrumentState.AssayFinishOrdered or
            InstrumentState.AssayFinishing)
        {
            var disableDialogPreventTitle = string.Format(@Loc["InstrumentsPage_DisableDialogPreventTitle"], instrument.DisplayName);
            var modal = ModalService.Show<InstrumentPreventDisableView>(new ModalOptions { Title = disableDialogPreventTitle, ModalHeight = ModalSize.XSmall, ModalWidth = ModalSize.Medium });

            // Wait for the modal to close
            await modal.Result;
        }
        else
        {
            var disableDialogTitle = string.Format(@Loc["InstrumentsPage_DisableDialogTitle"], instrument.DisplayName);

            var modal = ModalService.Show<InstrumentDisableView>(new ModalOptions { Title = disableDialogTitle, ModalHeight = ModalSize.XSmall, ModalWidth = ModalSize.Medium });

            var result = await modal.Result;

            if (!result.Canceled)
            {
                await UpdateInstrumentEnabled(instrument, false);
            }
        }
    }

    private async Task OnEnableInstrument(Instrument instrument)
    {
        var enableDialogTitle = string.Format(@Loc["InstrumentsPage_EnableDialogTitle"], instrument.DisplayName);

        var modal = ModalService.Show<InstrumentEnableView>(new ModalOptions { Title = enableDialogTitle, ModalHeight = ModalSize.XSmall, ModalWidth = ModalSize.Medium });

        var result = await modal.Result;

        if (!result.Canceled)
        {
            await UpdateInstrumentEnabled(instrument, true);
        }
    }

    private async Task OnDeleteInstrument(Instrument instrument)
    {
        var nrOfResults = await InstrumentsViewModel.GetTestResultsCount(instrument.SerialNumber);
        var deleteDialogResultsExistsText = string.Format(@Loc["InstrumentsPage_DeleteDialogResultExistsText"], nrOfResults);

        var deleteDialogTitle = string.Format(@Loc["InstrumentsPage_DeleteDialogTitle"], instrument.DisplayName);

        var parameters = new ModalParameters
                    {
                        { nameof(InstrumentDeleteView.DisplayName), instrument.DisplayName },
                        { nameof(InstrumentDeleteView.SerialNumber), instrument.SerialNumber },
                        { nameof(InstrumentDeleteView.ResultsExistsText), deleteDialogResultsExistsText }
                    };

        var modal = ModalService.Show<InstrumentDeleteView>(new ModalOptions { Title = deleteDialogTitle }, parameters);

        var result = await modal.Result;

        if (!result.Canceled)
        {
            await InstrumentsViewModel.DeleteInstrument(instrument.Id);
        }
    }

    private async Task OnRenameInstrument(Instrument instrument)
    {
        var parameters = new ModalParameters
                    {
                        { nameof(InstrumentRenameView.DisplayName), instrument.DisplayName },
                    };

        var modal = ModalService.Show<InstrumentRenameView>(
            new ModalOptions
            {
                Title = Loc["InstrumentsPage_RenameDialogTitle"],
                ModalHeight = ModalSize.Small,
                ModalWidth = ModalSize.Small

            }, parameters);

        var result = await modal.Result;

        if (!result.Canceled)
        {
            var data = result?.Data?.ToString();
            if (data != null)
            {
                await UpdateInstrumentName(instrument, data);
            }
        }
    }

    private async Task OnSelectTechnologies(Instrument instrument)
    {
        var viewModel = new SelectTechnologiesViewModel(instrument.Id, InstrumentTechnologyDataService);
        var modal = ModalService.Show<SelectTechnologiesView>(
            new ModalOptions
            {
                Title = Loc["InstrumentsPage_SelectTechnologiesDialogTitle"],
                ModalHeight = ModalSize.Medium,
                ModalWidth = ModalSize.Small
            },
            new ModalParameters
            {
                { nameof(SelectTechnologiesView.ViewModel), viewModel }
            });

        var result = await modal.Result;
        if (!result.Canceled)
        {
            await ReloadInstrumentsAsync();
        }
    }

    private async Task UpdateInstrumentEnabled(Instrument instrument, bool newValue)
    {
        instrument.Enabled = newValue;
        await InstrumentsViewModel.UpdateInstrument(instrument);
    }

    private async Task UpdateInstrumentName(Instrument instrument, string updatedDisplayName)
    {
        if (updatedDisplayName != instrument.DisplayName)
        {
            instrument.DisplayName = updatedDisplayName;
            await InstrumentsViewModel.UpdateInstrument(instrument);
        }
    }

    /// <summary>
    /// Get the assay scheduler text (for instrument card).
    /// </summary>
    /// <param name="isActive"></param>
    /// <param name="nextRunType"></param>
    /// <param name="assaySchedulerNextRun"></param>
    /// <param name="pausedUntilDate"></param>
    /// <returns></returns>
    private (string TextBeforeIcon, string TextAfterIcon) GetAssaySchedulerTexts(
        bool isActive,
        AssaySchedulerNextRunType nextRunType,
        DateTimeOffset? assaySchedulerNextRun,
        DateTimeOffset? pausedUntilDate
        )
    {
        var textAssaySchedule = Loc["InstrumentsPage_AssayScheduler_TextHeader"];
        if (isActive)
        {
            var dayToDisplay = GetShortDayName(assaySchedulerNextRun);
            var timeToDisplay = assaySchedulerNextRun?.ToString("t", CultureInfo.CurrentCulture) ?? string.Empty;

            return nextRunType != AssaySchedulerNextRunType.None
                ? (textAssaySchedule, dayToDisplay + ", " + timeToDisplay)
                : (textAssaySchedule, "");
        }

        // If the assay scheduler is not active, it is paused.
        return pausedUntilDate is null
            ? (textAssaySchedule, Loc["InstrumentsPage_AssayScheduler_TextPausedUntilManually"])
            : (textAssaySchedule, string.Format(Loc["InstrumentsPage_AssayScheduler_TextPausedUntilTime"], pausedUntilDate.Value.Date.ToShortDateString()));
    }

    private static HealthStatus GetHealthStatus(Instrument instrument)
    {
        return HealthStatusMapper.Map(instrument.SignalTower.LightState, instrument.State);
    }

    /// <summary>
    /// Get the short day name for the given date.
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public string GetShortDayName(DateTimeOffset? dateTime)
    {
        if (dateTime == null)
        {
            return string.Empty;
        }

        var now = DateTimeOffset.Now;
        if (dateTime.Value.Date == now.Date)
        {
            return Loc["InstrumentsPage_Day_Today"];
        }

        var culture = CultureInfo.CurrentCulture;
        var systemDay = dateTime.Value.DayOfWeek;
        var shortDay = culture.DateTimeFormat.GetAbbreviatedDayName(systemDay);
        return shortDay;
    }

    /// <summary>
    /// Convert the assay scheduler type (start, end or not set) to an icon type.
    /// </summary>
    /// <param name="isActive"></param>
    /// <param name="nextRunType"></param>
    /// <returns></returns>
    public static AssaySchedulerIconType ToAssaySchedulerIconType(bool isActive, AssaySchedulerNextRunType nextRunType)
    {
        if (!isActive)
        {
            return AssaySchedulerIconType.None;
        }

        return nextRunType switch
        {
            AssaySchedulerNextRunType.Start => AssaySchedulerIconType.Sun,
            AssaySchedulerNextRunType.End => AssaySchedulerIconType.Moon,
            AssaySchedulerNextRunType.None => AssaySchedulerIconType.None,
            _ => throw new ArgumentOutOfRangeException(nameof(nextRunType), nextRunType, null)
        };
    }

    /// <summary>
    /// Checks if an instrument is currently connected based on its state.
    /// </summary>
    /// <param name="instrument">The instrument to check.</param>
    /// <returns>True if the instrument is connected, false otherwise.</returns>
    private static bool IsInstrumentConnected(Instrument instrument)
    {
        return instrument.State != InstrumentState.Offline;
    }

    public void Dispose()
    {
        NotificationProvider.AssaySchedulerChanged -= OnAssaySchedulerChanged;
        NotificationProvider.AssaySchedulerDayChanged -= OnAssaySchedulerDayChanged;
        NotificationProvider.AssaySchedulerNextRunChanged -= OnAssaySchedulerNextRunChanged;
        NotificationProvider.InstrumentStateChanged -= OnInstrumentStateChanged;
        NotificationProvider.TimeWhenNextStateReachedChanged -= OnTimeWhenNextStateReachedChanged;
        NotificationProvider.InstrumentTechnologiesChanged -= OnInstrumentTechnologiesChanged;
        NotificationProvider.NumberOfTestsUpdated -= Refresh;
        NotificationProvider.SignalTowerStateChanged -= Refresh;
        TimerService.OnTimerTick -= OnTimerTick;
        InstrumentsViewModel.CollectionChanged -= OnItemCollectionChanged;
        TaskCardsListViewModel.PropertyChanged -= OnTaskCardsViewModelPropertyChanged;

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Gets localized state rows for an instrument card.
    /// </summary>
    /// <param name="instrument">The instrument to build rows for.</param>
    /// <returns>The two state/info rows shown in the card.</returns>
    private (string StateRow1, string StateRow2) GetInstrumentStateRows(Instrument instrument)
    {
        var rowData = InstrumentStateHelper.GetInstrumentStateRowsData(
            instrument.Enabled,
            instrument.State,
            instrument.NumberOfTests,
            instrument.TimeWhenNextStateReached);

        return InstrumentStateRowsFormatter.Format(rowData, Loc);
    }

    private List<string> GetTechnologies(Instrument instrument)
    {
        var selectedTechnologies = instrument.SelectedTechnologies?.ToList() ?? [];
        return selectedTechnologies.Count == 0
            ? [Loc["InstrumentsPage_Technologies_None"]]
            : selectedTechnologies.Select(x => x.ToLocalizedString("MethodDetailsGeneralTab")).ToList();
    }

    private Task ReloadInstrumentsAsync()
    {
        if (_instrumentsLoadTask is { IsCompleted: false })
        {
            return _instrumentsLoadTask;
        }

        _instrumentsLoadTask = InstrumentsViewModel.LoadAsync();
        return _instrumentsLoadTask;
    }
}
