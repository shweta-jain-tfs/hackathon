using CustomAction.CustomActionLogic.Interfaces.AppSettings;
using WixToolset.Dtf.WindowsInstaller;

namespace CustomAction.CustomActionLogic.Appsettings;
internal class AppSettingsLocator : IAppSettingsLocator
{
    public AppSettingsLocator(CustomActionData caData)
    {
        AppSettingsPath = caData["AppSettingsPath"];
    }
    public string AppSettingsPath { get; }
}
