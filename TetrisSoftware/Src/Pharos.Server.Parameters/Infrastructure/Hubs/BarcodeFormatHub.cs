using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Pharos.Contracts.Notifications;
using Pharos.Server.Infrastructure.Hubs;

namespace Pharos.Server.Parameters.Infrastructure.Hubs;

[Authorize]
public class BarcodeFormatHub(IHubContext<BarcodeFormatHub, IBarcodeFormatHub> hubContext, ILogger<BarcodeFormatHub> logger) : EntityHub<BarcodeFormatHub, IBarcodeFormatHub>(hubContext, logger), IBarcodeFormatHub
{
    protected override string DefaultGroup => "barcodeformat_default";
}