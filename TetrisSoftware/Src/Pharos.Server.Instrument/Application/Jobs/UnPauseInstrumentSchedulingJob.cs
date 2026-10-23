using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Instrument.Application.Commands.UpdateAssayScheduler;
using Pharos.Server.Instrument.Application.Events;
using Quartz;

namespace Pharos.Server.Instrument.Application.Jobs;

internal class UnPauseInstrumentSchedulingJob : IJob
{
    private readonly ILogger<UnPauseInstrumentSchedulingJob> _logger;
    private readonly ISender _sender;

    public UnPauseInstrumentSchedulingJob(ISender sender, ILogger<UnPauseInstrumentSchedulingJob> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public static string InstrumentId => "InstrumentId";

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var instrumentId = context.MergedJobDataMap.GetIntValue(InstrumentId);

            var command = new UpdateAssaySchedulerCommand
            {
                InstrumentId = instrumentId,
                IsActive = true,
                PausedUntilDate = null
            };

            await _sender.Send(command, context.CancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to execute UnPauseInstrumentSchedulingJob");
        }
    }

    public static JobKey Key(string instrumentNumber) { return new JobKey($"unpause-instrument-schedule-key-{instrumentNumber}", AssaySchedulingGroup.Key); }
}