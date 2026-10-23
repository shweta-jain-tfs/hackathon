using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Pharos.Contracts.Notifications;
using Pharos.Server.Infrastructure.Hubs;

namespace Pharos.Server.SystemErrors.Infrastructure.Hubs;

[Authorize]
public class SystemErrorHub(IHubContext<SystemErrorHub, IEntityHub> hubContext, ILogger<SystemErrorHub> logger) : EntityHub<SystemErrorHub, IEntityHub>(hubContext, logger)
{
    protected override string DefaultGroup => "system_error_default";
}