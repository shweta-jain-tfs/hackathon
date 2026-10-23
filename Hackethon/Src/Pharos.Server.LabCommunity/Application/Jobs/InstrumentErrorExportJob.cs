using MediatR;
using Pharos.Server.LabCommunity.Application.Commands;
using Pharos.Server.LabCommunity.Application.Interfaces;
using Pharos.Server.LabCommunity.Domain;
using Quartz;

namespace Pharos.Server.LabCommunity.Application.Jobs;

[DisallowConcurrentExecution]
internal sealed class InstrumentErrorExportJob(ISender sender) : ILabCommunityJob
{
    public static JobKey Key => new("labcommunity-instrument-error-export");
    public static TriggerKey Trigger => new("labcommunity-instrument-error-export-recurring-trigger");
    public static bool ShouldReconfigureAfterConfigurationPull(Configuration? newConfiguration, Configuration? previousConfiguration)
    {
        return newConfiguration is not null
               && (previousConfiguration is null
                   || previousConfiguration.ReportInstrumentErrorsEnabled != newConfiguration.ReportInstrumentErrorsEnabled);
    }

    public static bool ShouldStop(LabCommunityDefaultConfiguration defaultConfiguration, Configuration? configuration)
    {
        if (configuration is not null)
        {
            return !configuration.ReportInstrumentErrorsEnabled;
        }

        return true;
    }

    public static TimeSpan ResolveInterval(LabCommunityDefaultConfiguration defaultConfiguration, Configuration? configuration)
    {
        return defaultConfiguration.InstrumentErrorExportInterval;
    }

    public Task Execute(IJobExecutionContext context)
    {
        return sender.Send(new SendExportItemsToLabCommunityCommand(ExportItemType.InstrumentError), context.CancellationToken);
    }
}