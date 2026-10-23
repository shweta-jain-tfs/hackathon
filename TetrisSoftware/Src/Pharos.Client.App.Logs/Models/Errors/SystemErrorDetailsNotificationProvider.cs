using Pharos.Client.Common.Models.Notifications;
using Pharos.Client.Common.Models.Services;
using Pharos.Contracts.Notifications;

namespace Pharos.Client.App.Logs.Models.Errors;

internal sealed class SystemErrorDetailsNotificationProvider : EntityNotificationProvider<SystemErrorDetails>
{
    public SystemErrorDetailsNotificationProvider(IHubConnectionFactory hubConnectionFactory) : base(hubConnectionFactory)
    {
    }

    protected override string HubRoute => HubRoutes.SystemError;
}
