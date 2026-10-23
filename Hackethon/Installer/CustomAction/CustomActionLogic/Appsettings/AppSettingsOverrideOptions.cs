using CustomAction.CustomActionLogic.Interfaces.AppSettings;
using WixToolset.Dtf.WindowsInstaller;

namespace CustomAction.CustomActionLogic.Appsettings;
internal class AppSettingsOverrideOptions : IAppSettingsOverrideOptions
{
    public AppSettingsOverrideOptions(CustomActionData caData)
    {
        EnableDataProtection = caData["EnableDataProtection"];
    }
    public string EnableDataProtection { get; }
}
