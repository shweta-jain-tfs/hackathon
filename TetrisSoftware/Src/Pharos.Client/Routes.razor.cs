using Pharos.Client.Services;
using Pharos.Common.Web.Contexts;

namespace Pharos.Client;

public partial class Routes
{
    [Parameter] public ClientDeviceType ClientDeviceType { get; set; }
    [Inject] private IServiceProvider ServiceProvider { get; set; } = default!;

    private UiContext _uiContext = null!;

    protected override void OnParametersSet()
    {
        var clientDeviceService = ServiceProvider.GetService<ClientDeviceService>();
        clientDeviceService?.ClientDeviceType = ClientDeviceType;
        
        BuildUiContext();
    }

    private void BuildUiContext()
    {
        var uiComponentLoc = ServiceProvider.GetService<IUiComponentStringsLocalizer>();

        _uiContext = new UiContext
        {
            Device = MapClientDeviceType(ClientDeviceType),
            ComponentStrings = uiComponentLoc?.Build() ?? UiComponentStrings.Default
        };
    }

    private static DeviceType MapClientDeviceType(ClientDeviceType clientDeviceType)
    {
        return clientDeviceType switch
        {
            ClientDeviceType.Desktop => DeviceType.Desktop,
            ClientDeviceType.Instrument => DeviceType.Instrument,
            ClientDeviceType.Tablet => DeviceType.Tablet,
            ClientDeviceType.Phone => DeviceType.Phone,
            _ => DeviceType.Desktop
        };
    }
}