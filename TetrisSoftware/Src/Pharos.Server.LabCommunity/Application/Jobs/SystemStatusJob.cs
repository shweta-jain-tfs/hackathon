using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.LabCommunity.Application.Commands;
using Pharos.Server.LabCommunity.Application.Interfaces;
using Pharos.Server.LabCommunity.Domain;
using Quartz;

namespace Pharos.Server.LabCommunity.Application.Jobs;

[DisallowConcurrentExecution]
internal sealed class SystemStatusJob(
    ISender sender,
    ILogger<SystemStatusJob> logger) : ILabCommunityJob
{
    public static JobKey Key => new("labcommunity-system-status");

    public static TriggerKey Trigger => new("labcommunity-system-status-recurring-trigger");

    public static bool ShouldReconfigureAfterConfigurationPull(Configuration? newConfiguration, Configuration? previousConfiguration)
    {
        return newConfiguration is not null
               && (previousConfiguration is null
                   || previousConfiguration.SystemStatusEnabled != newConfiguration.SystemStatusEnabled
                   || previousConfiguration.SystemStatusIntervalSeconds != newConfiguration.SystemStatusIntervalSeconds);
    }

    public static bool ShouldStop(LabCommunityDefaultConfiguration defaultConfiguration, Configuration? configuration)
    {
        if (configuration is not null)
        {
            return !configuration.SystemStatusEnabled;
        }

        return !defaultConfiguration.SystemStatusEnabled;
    }

    public static TimeSpan ResolveInterval(LabCommunityDefaultConfiguration defaultConfiguration, Configuration? configuration)
    {
        if (configuration is not null)
        {
            return TimeSpan.FromSeconds(configuration.SystemStatusIntervalSeconds);
        }

        return defaultConfiguration.SystemStatusInterval;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await SendSystemStatusSlice(new SendPharosStatusToLabCommunityCommand(), "Pharos status", context.CancellationToken);
        await SendSystemStatusSlice(new SendInstrumentStatusToLabCommunityCommand(), "instrument status", context.CancellationToken);
    }

    private async Task SendSystemStatusSlice<TCommand>(TCommand command, string sliceName, CancellationToken cancellationToken)
        where TCommand : IRequest
    {
        try
        {
            await sender.Send(command, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Sending LabCommunity SystemStatus ({SliceName}) failed.", sliceName);
        }
    }
}