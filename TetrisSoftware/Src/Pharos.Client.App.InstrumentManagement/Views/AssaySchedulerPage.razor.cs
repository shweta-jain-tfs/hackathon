using System.Collections.Specialized;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.InstrumentManagement.Models.AssayScheduler;
using Pharos.Client.App.InstrumentManagement.Models.Instruments;
using Pharos.Client.App.InstrumentManagement.ViewModels.AssayScheduler;
using Pharos.Client.App.InstrumentManagement.Views.AssayScheduler;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.PageRouting;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using Pharos.Common.Web.Components.Breadcrumb;
using Pharos.Common.Web.Components.Modal;
using Pharos.Common.Web.Components.Popover;
using Pharos.Common.Web.Utilities;
using Pharos.Contracts.Security;
using DayOfWeek = Pharos.Client.Common.Models.DayOfWeek;

namespace Pharos.Client.App.InstrumentManagement.Views;

public partial class AssaySchedulerPage : IDisposable
{
    [Inject] private AssaySchedulerDayDataService DayDataService { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private IModalService Modal { get; set; } = default!;
    [Inject] public IInstrumentNotificationProvider NotificationProvider { get; set; } = default!;
    [Inject] public IAuthorizationService AuthorizationService { get; set; } = default!;
    [Inject] private AssaySchedulerViewModel ViewModel { get; set; } = default!;

    private readonly Dictionary<DayOfWeek, PhPopover> _popovers = new();

    private bool AnyDirectlyScheduled =>
        ViewModel.Any(assayScheduler =>
            assayScheduler.AssaySchedulerDays.Any(day =>
                day.ScheduledMethods.Any(m =>
                    m.IsActive && (InstrumentMethodWhenToRun)m.WhenToRun == InstrumentMethodWhenToRun.Directly
                )
            )
        );

    private bool IsAuthorized { get; set; }

    /// <summary>
    /// Need to track selections for clearing days in the popover.
    /// </summary>
    private readonly Dictionary<(DayOfWeek Source, DayOfWeek Target), bool> _daySelections = new();

    private static List<BreadcrumbItem> Breadcrumbs =>
    [
        new("Instruments", PageRoute.Instruments),
        new("Assay scheduling", "")
    ];

    protected string CssClassCell => new CssBuilder("ph-assay-scheduler__cell")
        .AddClass("ph-assay-scheduler__cell--disabled", !IsAuthorized)
        .Build();

    protected override async Task OnInitializedAsync()
    {
        IsAuthorized = await AuthorizationService.IsAuthorized(Permissions.ManageInstruments);

        NotificationProvider.AssaySchedulerChanged += OnAssaySchedulerChanged;
        NotificationProvider.AssaySchedulerDayChanged += OnAssaySchedulerDayChanged;

        ViewModel.CollectionChanged += OnItemCollectionChanged;

        // Clear all selections.
        foreach (var sourceDay in AssaySchedulerViewModel.GetDaysOfWeek())
        {
            ClearAllSelections(sourceDay);
        }

        // Ensure all days have a popover ref.
        foreach (var day in AssaySchedulerViewModel.GetDaysOfWeek())
        {
            _popovers.TryAdd(day, null!);
        }

        await ViewModel.LoadAsync();
    }

    /// <summary>
    /// Clear selection for specific day.
    /// </summary>
    /// <param name="sourceDay"></param>
    private void ClearAllSelections(DayOfWeek sourceDay)
    {
        foreach (var targetDay in AssaySchedulerViewModel.GetDaysOfWeek())
        {
            _daySelections[(sourceDay, targetDay)] = false;
        }
    }

    /// <summary>
    /// Determines whether the copy operation is enabled for the specified day of the week.
    /// </summary>
    /// <param name="sourceDay"></param>
    private bool IsCopyEnabled(DayOfWeek sourceDay)
    {
        return IsAuthorized && ViewModel.IsCopyEnabledForDay(sourceDay);
    }

    private void OnAssaySchedulerChanged(int instrumentId, int assaySchedulerId, bool assaySchedulerIsActive, DateTimeOffset? assaySchedulerPausedUntilDate)
    {
        // Locate the AssayScheduler in the ViewModel.
        var assayScheduler = ViewModel.FirstOrDefault(i => i.Id == assaySchedulerId);

        if (assayScheduler == null)
        {
            // TODO: Error handling?
            return;
        }

        assayScheduler.IsActive = assaySchedulerIsActive;
        assayScheduler.PausedUntilDate = assaySchedulerPausedUntilDate;
    }

    /// <summary>
    /// Handles the event when an AssaySchedulerDay is changed.
    /// </summary>
    /// <param name="instrumentId"></param>
    /// <param name="assaySchedulerId"></param>
    /// <param name="day"></param>
    /// <param name="anyDayHasTime"></param>
    private void OnAssaySchedulerDayChanged(int instrumentId, int assaySchedulerId, AssaySchedulerDay day, bool anyDayHasTime)
    {
        // Locate the AssayScheduler in the ViewModel.
        var assayScheduler = ViewModel.FirstOrDefault(i => i.Id == assaySchedulerId);

        // Locate the specific day entry that matches the changed day of week.
        var assaySchedulerDay = assayScheduler?.AssaySchedulerDays.FirstOrDefault(x => x.Day == day.Day);
        if (assaySchedulerDay == null)
        {
            // TODO: Error handling?
            return;
        }

        assaySchedulerDay.StartTime = day.StartTime;
        assaySchedulerDay.EndTime = day.EndTime;

        foreach (var method in assaySchedulerDay.ScheduledMethods)
        {
            var newMethod = day.ScheduledMethods.FirstOrDefault(x => x.MethodName == method.MethodName);
            if (newMethod == null)
            {
                // TODO: Error handling?
                continue;
            }

            method.IsActive = newMethod.IsActive;
            method.WhenToRun = newMethod.WhenToRun;
        }

        // Notify the UI to re-render.
        StateHasChanged();
    }

    /// <summary>
    /// Opens a modal dialog for editing the assay scheduler configuration for a specific day of the week.
    /// </summary>
    /// <param name="assayScheduler"></param>
    /// <param name="dayOfWeek"></param>
    /// <returns></returns>
    private async Task OpenAssayScheduler(Common.Models.AssayScheduler assayScheduler, DayOfWeek dayOfWeek)
    {
        if (!IsAuthorized)
        {
            return;
        }

        // Find the AssaySchedulerDay for the given day.
        var assaySchedulerDay = assayScheduler.AssaySchedulerDays.FirstOrDefault(x => x.Day == dayOfWeek);

        if (assaySchedulerDay != null)
        {
            var copiedModel = new AssaySchedulerDay
            {
                Day = assaySchedulerDay.Day,
                Id = assaySchedulerDay.Id,
                StartTime = assaySchedulerDay.StartTime,
                EndTime = assaySchedulerDay.EndTime,
                ScheduledMethods = assaySchedulerDay.ScheduledMethods
                    .Select(method => new AssaySchedulerMethod
                    {
                        MethodName = method.MethodName,
                        WhenToRun = method.WhenToRun,
                        IsActive = method.IsActive
                    })
                    .ToList()
            };
            var copiedViewModel = new AssaySchedulerDayViewModel(copiedModel, assayScheduler.InstrumentName);

            var parameters = new ModalParameters
            {
                { nameof(AssaySchedulerDayModalView.ViewModel), copiedViewModel },
            };
            var modal = Modal.Show<AssaySchedulerDayModalView>(new ModalOptions
            {
                Title = Loc["AssaySchedulerPage_AssaySchedulerDayModal_Title"],
                ModalHeight = ModalSize.XLarge,
                ModalWidth = ModalSize.Medium
            }, parameters);

            var result = await modal.Result;
            if (result is { Canceled: false, Data: AssaySchedulerDayViewModel updatedViewModel })
            {
                await DayDataService.UpdateAsync(updatedViewModel.Model);

                // Update the original data with the changes from the updated ViewModel.
                UpdateOriginalData(assaySchedulerDay, updatedViewModel);
                StateHasChanged();
            }

            copiedViewModel.Dispose();
        }
    }

    /// <summary>
    /// Update the original data with the changes from the updated ViewModel.
    /// </summary>
    /// <param name="originalDay"></param>
    /// <param name="updatedViewModel"></param>
    private static void UpdateOriginalData(AssaySchedulerDay originalDay, AssaySchedulerDayViewModel updatedViewModel)
    {
        originalDay.StartTime = updatedViewModel.StartTime;
        originalDay.EndTime = updatedViewModel.EndTime;

        // Update ScheduledMethods.
        originalDay.ScheduledMethods.Clear();
        foreach (var method in updatedViewModel.ScheduledMethods)
        {
            originalDay.ScheduledMethods.Add(new AssaySchedulerMethod
            {
                MethodName = method.MethodName,
                IsActive = method.IsActive,
                WhenToRun = (int)method.WhenToRun,
            });
        }
    }

    private void OnItemCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        StateHasChanged();
    }

    /// <summary>
    /// Opens the area where the user can copy the day settings from one day to another.
    /// </summary>
    /// <param name="isOpen"></param>
    /// <param name="day"></param>
    private void CopyDayOpen(bool isOpen, DayOfWeek day)
    {
        if (isOpen)
        {
            ClearAllSelections(day);
            ViewModel.SnapshotOriginalDayValues();
        }
    }

    public void Dispose()
    {
        NotificationProvider.AssaySchedulerChanged -= OnAssaySchedulerChanged;
        NotificationProvider.AssaySchedulerDayChanged -= OnAssaySchedulerDayChanged;

        ViewModel.CollectionChanged -= OnItemCollectionChanged;

        GC.SuppressFinalize(this);
    }
}
