using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Pharos.Contracts.Notifications;
using Pharos.Contracts.Security;
using Pharos.Server.Infrastructure.Hubs;

namespace Pharos.Server.TaskCard.Infrastructure.Hubs;

[Authorize(Policies.InstrumentOnlyAllowAnonymous)]
public class TaskCardHub(IHubContext<TaskCardHub, ITaskCardHub> hubContext, ILogger<TaskCardHub> logger) : HubBase<TaskCardHub, ITaskCardHub>(hubContext, logger), ITaskCardHub
{
    protected override string DefaultGroup => "task_card_default";

    public Task TaskCardsUpdated()
    {
        return HubContext.Clients.Group(DefaultGroup).TaskCardsUpdated();
    }
}