using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Pharos.Contracts.Notifications;

namespace Pharos.Server.Infrastructure.Hubs;

[Authorize]
public class JobHub(
    IHubContext<JobHub, IJobHub> hubContext,
    ILogger<HubBase<JobHub, IJobHub>> logger) : HubBase<JobHub, IJobHub>(hubContext, logger), IJobHub
{
    protected override string DefaultGroup => "jobs";

    public Task JobCompleted(string jobName, Guid startedBy, bool isSuccessful, string resultMessage)
    {
        return HubContext.Clients.Group(DefaultGroup).JobCompleted(jobName, startedBy, isSuccessful, resultMessage);
    }
}
