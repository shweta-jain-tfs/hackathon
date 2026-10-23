using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Settings.Instrument;
using Pharos.Server.Domain.Enums;
using Quartz;

namespace Pharos.Server.Instrument.Application.Services;

internal sealed class AutomaticPowerSaveScheduler(
    ISchedulerFactory schedulerFactory,
    IOptionsMonitor<InstrumentSettings> settings,
    ILogger<AutomaticPowerSaveScheduler> logger) : IAutomaticPowerSaveScheduler
{
    private enum LoginStatus
    {
        LoggedIn,
        LoggedOut
    }

    private sealed class InstrumentPowerSaveState
    {
        public LoginStatus LoginStatus { get; set; } = LoginStatus.LoggedOut;
        public InstrumentState LastKnownState { get; set; } = InstrumentState.Offline;
    }

    private readonly ConcurrentDictionary<int, InstrumentPowerSaveState> _states = new();

    public async Task OnInstrumentStateChangedAsync(int instrumentId, InstrumentState state, CancellationToken cancellationToken)
    {
        _states.AddOrUpdate(
            instrumentId,
            _ => new InstrumentPowerSaveState { LastKnownState = state },
            (_, existing) =>
            {
                existing.LastKnownState = state;
                return existing;
            });

        await UpdateScheduleAsync(instrumentId, cancellationToken);
    }

    public Task MarkLoggedInAsync(int instrumentId, CancellationToken cancellationToken)
    {
        return UpdateLoginStateAsync(instrumentId, LoginStatus.LoggedIn, cancellationToken);
    }

    public Task MarkLoggedOutAsync(int instrumentId, CancellationToken cancellationToken)
    {
        return UpdateLoginStateAsync(instrumentId, LoginStatus.LoggedOut, cancellationToken);
    }

    private async Task UpdateLoginStateAsync(int instrumentId, LoginStatus loginStatus, CancellationToken cancellationToken)
    {
        _states.AddOrUpdate(
            instrumentId,
            _ => new InstrumentPowerSaveState { LoginStatus = loginStatus },
            (_, existing) =>
            {
                existing.LoginStatus = loginStatus;
                return existing;
            });

        await UpdateScheduleAsync(instrumentId, cancellationToken);
    }

    private async Task UpdateScheduleAsync(int instrumentId, CancellationToken cancellationToken)
    {
        var scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        var jobKey = AutomaticPowerSaveJob.Key(instrumentId);

        if (!settings.CurrentValue.EnableAutomaticPowerSave)
        {
            await scheduler.DeleteJob(jobKey, cancellationToken);
            return;
        }

        if (!ShouldSchedule(instrumentId))
        {
            var deleted = await scheduler.DeleteJob(jobKey, cancellationToken);
            if (deleted && _states.TryGetValue(instrumentId, out var currentState))
            {
                logger.LogInformation(
                    "Automatic power save cancelled for instrument {InstrumentId}. Conditions no longer met (state={State}, login={Login}).",
                    instrumentId, currentState.LastKnownState, currentState.LoginStatus);
            }
            return;
        }

        await scheduler.DeleteJob(jobKey, cancellationToken);

        var timeout = settings.CurrentValue.PowerSaveTimeoutMinutes;
        var job = JobBuilder.Create<AutomaticPowerSaveJob>()
            .WithIdentity(jobKey)
            .UsingJobData(AutomaticPowerSaveJob.InstrumentIdKey, instrumentId)
            .Build();

        var trigger = TriggerBuilder.Create()
            .ForJob(job)
            .StartAt(DateBuilder.FutureDate(timeout, IntervalUnit.Minute))
            .Build();

        await scheduler.ScheduleJob(job, trigger, cancellationToken);
        logger.LogInformation("Automatic power save scheduled for instrument {InstrumentId} in {Timeout} minute(s).", instrumentId, timeout);
    }

    private bool ShouldSchedule(int instrumentId)
    {
        if (!_states.TryGetValue(instrumentId, out var state) || state.LoginStatus != LoginStatus.LoggedOut)
        {
            return false;
        }

        return state.LastKnownState == InstrumentState.Standby;
    }
}
