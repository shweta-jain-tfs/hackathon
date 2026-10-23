using Pharos.Server.LabCommunity.Domain;
using Quartz;

namespace Pharos.Server.LabCommunity.Application.Interfaces;

internal interface ILabCommunityJob : IJob
{
    static abstract JobKey Key { get; }

    static abstract TriggerKey Trigger { get; }

    static abstract bool ShouldReconfigureAfterConfigurationPull(Configuration? newConfiguration, Configuration? previousConfiguration);

    static abstract bool ShouldStop(LabCommunityDefaultConfiguration defaultConfiguration, Configuration? configuration);

    static abstract TimeSpan ResolveInterval(LabCommunityDefaultConfiguration defaultConfiguration, Configuration? configuration);
}
