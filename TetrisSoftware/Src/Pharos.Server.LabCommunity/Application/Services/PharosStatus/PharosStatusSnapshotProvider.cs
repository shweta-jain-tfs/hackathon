using LabCommunity.API.Client;
using Pharos.Server.Application;
using Pharos.Server.Infrastructure;
using Pharos.Server.LabCommunity.Application.Interfaces.PharosStatus;

namespace Pharos.Server.LabCommunity.Application.Services.PharosStatus;

internal sealed class PharosStatusSnapshotProvider(
    IPharosVersionService versionService,
    IDateTime dateTime,
    IOperatingSystemInformation operatingSystemInformation) : IPharosStatusSnapshotProvider
{
    public OperatorSoftwareStatusRequest CreateSnapshot()
    {
        return new OperatorSoftwareStatusRequest
        {
            ReportedAt = dateTime.UtcNow,
            Software = new OperatorSoftwareStatus
            {
                Version = versionService.Version,
                RunningSince = versionService.StartedAt
            },
            OperatingSystem = new OperatingSystemStatus
            {
                Version = operatingSystemInformation.Version,
                FriendlyName = operatingSystemInformation.FriendlyName
            }
        };
    }
}
