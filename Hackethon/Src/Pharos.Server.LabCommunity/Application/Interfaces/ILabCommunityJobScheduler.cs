using Pharos.Server.LabCommunity.Domain;

namespace Pharos.Server.LabCommunity.Application.Interfaces;

internal interface ILabCommunityJobScheduler
{
    bool ShouldReconfigureAfterConfigurationPull(Configuration? newConfiguration, Configuration? previousConfiguration);

    bool ShouldStop(LabCommunityDefaultConfiguration defaultConfiguration, Configuration? configuration);

    TimeSpan ResolveInterval(LabCommunityDefaultConfiguration defaultConfiguration, Configuration? configuration);

    Task ConfigureScheduler(TimeSpan interval, CancellationToken cancellationToken, TimeSpan initialDelay = default);

    Task StopScheduler(CancellationToken cancellationToken);
}
