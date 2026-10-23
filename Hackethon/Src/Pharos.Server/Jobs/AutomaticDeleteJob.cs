using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Request;
using Pharos.Server.Application.Result;
using Pharos.Server.Application.Settings.Database;
using Quartz;

namespace Pharos.Server.Jobs;

internal class AutomaticDeleteJob : IJob
{
    private readonly ILogger<AutomaticDeleteJob> _logger;
    private readonly ISender _sender;
    private readonly AutomaticDeleteSettings _settings;

    public AutomaticDeleteJob(ISender sender, IOptionsSnapshot<AutomaticDeleteSettings> settings, ILogger<AutomaticDeleteJob> logger)
    {
        _sender = sender;
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            _logger.LogInformation("AutomaticDeleteJob started");

            if (_settings.DeleteRequestsAutomatically)
            {
                await _sender.Send(new CleanupRequestsCommand(_settings.DeleteRequestsOlderThanDays));
            }
            if (_settings.DeleteResultsAutomatically)
            {
                await _sender.Send(new CleanupResultsCommand(_settings.DeleteResultsOlderThanDays));
            }
            if (_settings.DeleteQualityAutomatically)
            {
                await _sender.Send(new CleanupQualityCommand(_settings.DeleteQualityOlderThanDays));
            }

            await _sender.Send(new CleanupCurrentCurvesCommand());

            _logger.LogInformation("AutomaticDeleteJob completed");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "AutomaticDeleteJob failed during execution");
        }
    }

    public static JobKey Key() { return new JobKey("automatic-delete-key"); }
}