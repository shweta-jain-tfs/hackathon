using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Instrument.Application.Commands.ControlInstrumentState;
using Pharos.Server.Instrument.Application.Events;
using Quartz;

namespace Pharos.Server.Instrument.Application.Jobs;

internal class EndAssayJob : IJob
{
    private readonly ILogger<EndAssayJob> _logger;
    private readonly ISender _sender;

    public EndAssayJob(ISender sender, ILogger<EndAssayJob> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public static string InstrumentNumber => "InstrumentNumber";

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var instrumentNumber = context.MergedJobDataMap.GetString(InstrumentNumber);

            await _sender.Send(new EndAssayCommand(instrumentNumber!, EndAssayFinishOptions.FinishAllSamplesScheduled), context.CancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to execute EndAssayJob");
        }
    }

    public static JobKey Key(string instrumentNumber, DayOfWeek day) { return new JobKey($"end-assay-key-{instrumentNumber}-{day}", AssaySchedulingGroup.Key); }
}