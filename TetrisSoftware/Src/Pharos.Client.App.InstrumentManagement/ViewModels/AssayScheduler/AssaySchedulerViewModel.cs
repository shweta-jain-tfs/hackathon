using System.Globalization;
using Pharos.Client.App.InstrumentManagement.Models.AssayScheduler;
using Pharos.Client.Common.Models;
using Pharos.Client.Common.Models.DataServices;
using Pharos.Client.Common.Models.Notifications;
using Pharos.Client.Common.ViewModels;
using DayOfWeek = Pharos.Client.Common.Models.DayOfWeek;

namespace Pharos.Client.App.InstrumentManagement.ViewModels.AssayScheduler;

public sealed class AssaySchedulerViewModel(
    IEntityDataService<Common.Models.AssayScheduler> dataSource,
    AssaySchedulerDayDataService dayDataService,
    IEntityNotificationProvider<Common.Models.AssayScheduler> notificationProvider)
    : DynamicEntityListViewModel<Common.Models.AssayScheduler>(dataSource, notificationProvider)
{
    /// <summary>
    /// Holds a snapshot of the original scheduling data for each day and scheduler.
    /// It is used to restore previous values if a user undoes a copy operation in the UI.
    /// </summary>
    private readonly Dictionary<(int Id, DayOfWeek TargetDay), (TimeOnly? StartTime, TimeOnly? EndTime, List<AssaySchedulerMethod> Methods)> _originalDayValues = new();


    /// <summary>
    /// Gets the days of the week in the order defined by the culture's first day of week.
    /// </summary>
    /// <param name="culture"></param>
    /// <returns></returns>
    public static IEnumerable<DayOfWeek> GetDaysOfWeek(CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;
        var firstDay = (int)culture.DateTimeFormat.FirstDayOfWeek;
        var days = Enum.GetValues<DayOfWeek>().ToList();

        // Rotate the list so it starts with the culture's first day of week.
        var orderedDays = days.Skip(firstDay).Concat(days.Take(firstDay)).ToList();
        return orderedDays;
    }

    /// <summary>
    /// Determines whether copying is enabled for the specified day of the week.
    /// </summary>
    /// <remarks>Copying is considered enabled for a day if any associated scheduler has active methods,  a
    /// defined start time, or a defined end time for that day.</remarks>
    /// <param name="day">The day of the week to check.</param>
    /// <returns><see langword="true"/> if copying is enabled for the specified day; otherwise, <see langword="false"/>.</returns>
    public bool IsCopyEnabledForDay(DayOfWeek day)
    {
        return this.Any(assayScheduler =>
            assayScheduler.AssaySchedulerDays.Any(d =>
                d.Day.Equals(day) &&
                (
                    d.ScheduledMethods.Any(m => m.IsActive)
                    || d.StartTime != null
                    || d.EndTime != null
                )
            )
        );
    }

    /// <summary>
    /// Copy all data from one source day to target day.
    /// </summary>
    /// <param name="sourceDay"></param>
    /// <param name="targetDay"></param>
    /// <param name="isChecked"></param>
    public void OnCopyDayChecked(DayOfWeek sourceDay, DayOfWeek targetDay, bool isChecked)
    {
        foreach (var scheduler in this)
        {
            var source = scheduler.AssaySchedulerDays.FirstOrDefault(d => d.Day == sourceDay);
            var target = scheduler.AssaySchedulerDays.FirstOrDefault(d => d.Day == targetDay);

            if (target == null)
            {
                continue;
            }

            var key = (scheduler.Id, targetDay);

            if (isChecked)
            {
                // Copy values from source to target.
                if (source != null)
                {
                    target.StartTime = source.StartTime;
                    target.EndTime = source.EndTime;

                    target.ScheduledMethods.Clear();
                    foreach (var m in source.ScheduledMethods)
                    {
                        target.ScheduledMethods.Add(new AssaySchedulerMethod
                        {
                            MethodName = m.MethodName,
                            IsActive = m.IsActive,
                            WhenToRun = m.WhenToRun,
                        });
                    }

                    // Save the copied values to DB.
                    _ = dayDataService.UpdateAsync(target);
                }
            }
            else
            {
                // Restore original values if available.
                if (_originalDayValues.TryGetValue(key, out var original))
                {
                    target.StartTime = original.StartTime;
                    target.EndTime = original.EndTime;

                    target.ScheduledMethods.Clear();
                    foreach (var m in original.Methods)
                    {
                        target.ScheduledMethods.Add(new AssaySchedulerMethod
                        {
                            MethodName = m.MethodName,
                            IsActive = m.IsActive,
                            WhenToRun = m.WhenToRun,
                        });
                    }

                    // Save the copied values to DB.
                    _ = dayDataService.UpdateAsync(target);
                }
            }
        }
    }

    public void SnapshotOriginalDayValues()
    {
        _originalDayValues.Clear();
        foreach (var scheduler in this)
        {
            foreach (var day in GetDaysOfWeek())
            {
                var target = scheduler.AssaySchedulerDays.FirstOrDefault(d => d.Day == day);
                if (target == null)
                {
                    continue;
                }

                var key = (scheduler.Id, day);

                // Only store if not already present (to avoid overwriting if user toggles multiple times)
                if (!_originalDayValues.ContainsKey(key))
                {
                    var originalMethods = target.ScheduledMethods
                        .Select(m => new AssaySchedulerMethod
                        {
                            MethodName = m.MethodName,
                            IsActive = m.IsActive,
                            WhenToRun = m.WhenToRun,
                        })
                        .ToList();

                    _originalDayValues[key] = (target.StartTime, target.EndTime, originalMethods);
                }
            }
        }
    }
}