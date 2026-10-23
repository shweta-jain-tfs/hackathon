using Microsoft.Extensions.Logging;
using Pharos.Server.Instrument.Application.Interfaces;
using Quartz;

namespace Pharos.Server.Instrument.Application.Jobs;

[DisallowConcurrentExecution]
internal class StopPairingJob(
    IInstrumentPairingService pairingService,
    ILogger<StopPairingJob> logger) : IJob
{
    public const string StartedBy = nameof(StartedBy);
    public const string UploadedFileId = nameof(UploadedFileId);

    public static readonly JobKey Key = new("stop-pairing");

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            await pairingService.DisablePairing();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Stop pairing failed");
        }
    }
}