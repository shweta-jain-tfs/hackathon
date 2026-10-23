using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Instrument.Application.Commands.ControlInstrumentState;
using Quartz;

namespace Pharos.Server.Instrument.Application.Services;

[DisallowConcurrentExecution]
internal sealed class AutomaticPowerSaveJob(IInstrumentManager instrumentManager, ISender sender, ILogger<AutomaticPowerSaveJob> logger) : IJob
{
    public const string InstrumentIdKey = nameof(InstrumentIdKey);

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var instrumentId = context.MergedJobDataMap.GetIntValue(InstrumentIdKey);
            logger.LogInformation("Automatic power save job started for instrument {InstrumentId}.", instrumentId);

            var instrument = instrumentManager.GetInstrument(instrumentId);
            if (instrument.State == InstrumentState.Standby)
            {
                await sender.Send(new PowerSaveCommand(instrument.SerialNumber), context.CancellationToken);
            }
            else
            {
                logger.LogDebug("Instrument {Instrument} is not in standby state. Skipping automatic power save.", instrument.SerialNumber);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Automatic power save failed during execution.");
        }
    }

    public static JobKey Key(int instrumentId)
    {
        return new($"automatic-power-save-{instrumentId}");
    }
}
