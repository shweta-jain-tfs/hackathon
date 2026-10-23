using Pharos.Server.LabCommunity.Domain;

namespace Pharos.Server.LabCommunity.Application.Interfaces;

internal interface ILabCommunitySchedulerService
{
    Task ConfigureAllFromCurrentConfiguration(
        string laboratoryCode,
        CancellationToken cancellationToken);

    Task ReconfigureAfterConfigurationPull(
        Configuration? newConfiguration,
        Configuration? previousConfiguration,
        CancellationToken cancellationToken);

    Task StopAll(CancellationToken cancellationToken);
}