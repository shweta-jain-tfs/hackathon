using Pharos.Server.Instrument.Application.Queries.GetInstruments;
using Pharos.Server.Instrument.Domain;

namespace Pharos.Server.Instrument.Application.Helpers
{
    public static class AssaySchedulerHelper
    {
        /// <summary>
        /// Calculates the next run date and time for the assay scheduler based on the current date and time.
        /// </summary>
        /// <param name="days"></param>
        /// <param name="now"></param>
        /// <returns></returns>
        internal static (DateTimeOffset? nextRunDateTime, AssaySchedulerNextRunType NextRunType) GetNextRunAssayScheduler(
            IEnumerable<AssaySchedulerDay> days,
            DateTimeOffset? now = null)
        {
            now ??= DateTimeOffset.Now;
            var today = now.Value.DayOfWeek;
            var currentTime = TimeOnly.FromDateTime(now.Value.DateTime);

            // Collect all start and end runs from the schedule.
            var runs = new List<(DayOfWeek day, TimeOnly time, AssaySchedulerNextRunType nextRunType)>();
            foreach (var day in days)
            {
                if (day.StartTime.HasValue)
                {
                    runs.Add((day.Day, day.StartTime.Value, AssaySchedulerNextRunType.Start));
                }

                if (day.EndTime.HasValue)
                {
                    runs.Add((day.Day, day.EndTime.Value, AssaySchedulerNextRunType.End));
                }
            }

            // Find the next run.
            var nextRun = runs
                .Select(dayRow =>
                {
                    // Calculate how many days until the run.
                    var daysUntil = ((int)dayRow.day - (int)today + 7) % 7;
                    var nextRunDate = now.Value.Date.AddDays(daysUntil);
                    var nextRunDateAndTime = nextRunDate + dayRow.time.ToTimeSpan();

                    // If the run is today but already passed, schedule for next week.
                    if (daysUntil == 0 && dayRow.time <= currentTime)
                    {
                        nextRunDateAndTime = nextRunDateAndTime.AddDays(7);
                    }

                    return new
                    {
                        nextRunDateTime = new DateTimeOffset(nextRunDateAndTime, now.Value.Offset),
                        dayRow.nextRunType
                    };
                })
                .OrderBy(e => e.nextRunDateTime).FirstOrDefault();

            if (nextRun == null)
            {
                return (null, AssaySchedulerNextRunType.None);
            }

            return (nextRun.nextRunDateTime, nextRun.nextRunType);
        }
    }
}
