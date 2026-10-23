using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Instrument.Application.Commands.ControlInstrumentState;
using Pharos.Server.Instrument.Application.Commands.SendActiveMethods;
using Pharos.Server.Instrument.Application.Events;
using Pharos.Server.Instrument.Application.Interfaces;
using Quartz;
using Tetris.Protocol.Pharos;

namespace Pharos.Server.Instrument.Application.Jobs;

internal class StartAssayJob : IJob
{
    private readonly IInstrumentDbContext _dbContext;
    private readonly ICurrentInstrumentServerHub _hub;
    private readonly IPublisher _publisher;
    private readonly ICommunicationSessionManager _sessionManager;
    private readonly ILogger<StartAssayJob> _logger;
    private readonly ISender _sender;

    public StartAssayJob(ISender sender, IPublisher publisher, IInstrumentDbContext dbContext, ICurrentInstrumentServerHub hub, ICommunicationSessionManager sessionManager, ILogger<StartAssayJob> logger)
    {
        _sender = sender;
        _publisher = publisher;
        _dbContext = dbContext;
        _hub = hub;
        _sessionManager = sessionManager;
        _logger = logger;
    }

    public static string InstrumentId => "InstrumentId";
    public static string InstrumentNumber => "InstrumentNumber";
    public static string SchedulerDayId => "SchedulerDayId";

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var instrumentNumber = context.MergedJobDataMap.GetString(InstrumentNumber);

            if (string.IsNullOrEmpty(instrumentNumber))
            {
                throw new InvalidOperationException("Instrument number is not provided in the job data map.");
            }

            // Get the scheduler day
            var schedulerDayId = context.MergedJobDataMap.GetIntValue(SchedulerDayId);
            var schedulerDay = await _dbContext.AssaySchedulerDays.Include(x => x.ScheduledMethods).AsNoTracking().FirstOrDefaultAsync(x => x.Id == schedulerDayId, context.CancellationToken);

            if (schedulerDay == null)
            {
                throw new InvalidOperationException($"Scheduler day with ID {schedulerDayId} not found.");
            }

            // Get all instrument methods for the instrument
            var instrumentId = context.MergedJobDataMap.GetIntValue(InstrumentId);
            var instrumentMethods = await _dbContext.InstrumentMethods.Include(x => x.Instrument).Where(x => x.Instrument.Id == instrumentId).ToListAsync(context.CancellationToken);

            var previousActivation = instrumentMethods.ToDictionary(x => x.MethodName, x => x.IsActive);

            instrumentMethods.ForEach(x => x.IsActive = false);

            // Update the methods based on the scheduled methods for the day
            foreach (var scheduledMethod in schedulerDay.ScheduledMethods)
            {
                var instrumentMethod = instrumentMethods.FirstOrDefault(x => x.MethodName == scheduledMethod.MethodName);
                if (instrumentMethod != null)
                {
                    instrumentMethod.IsActive = scheduledMethod.IsActive;
                    instrumentMethod.Calibrator = false;
                    instrumentMethod.ForceCalibrator = false;
                    instrumentMethod.WhenToRun = scheduledMethod.WhenToRun;
                }
            }

            var changedMethodActivation = instrumentMethods
                .Where(x => previousActivation.TryGetValue(x.MethodName, out var previous) && previous != x.IsActive)
                .Select(x => new { x.MethodName, x.IsActive })
                .ToList();

            await _dbContext.SaveChangesAsync(context.CancellationToken);

            // Only try to communicate with the instrument if there is an active session and connection
            var session = _sessionManager.GetSession(instrumentNumber);
            if (session is { CompatibilityStatus: EnumCompatibilityStatus.FullyCompatible })
            {
                // Send the active methods to the instrument
                await _sender.Send(new SendActiveMethodsCommand(instrumentNumber), context.CancellationToken);

                foreach (var method in changedMethodActivation)
                {
                    await _publisher.Publish(new MethodActivatedEvent(instrumentNumber, method.MethodName, method.IsActive), context.CancellationToken);
                }

                // Control the instrument state to start the assay, this will also kick the UpdateMethodStatusAllCommand
                await _sender.Send(new StartAssayCommand(instrumentNumber), context.CancellationToken);
            }

            await _hub.InstrumentMethodsChanged(instrumentId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to execute StartAssayJob");
        }
    }

    public static JobKey Key(string instrumentNumber, DayOfWeek day) { return new JobKey($"start-assay-key-{instrumentNumber}-{day}", AssaySchedulingGroup.Key); }
}