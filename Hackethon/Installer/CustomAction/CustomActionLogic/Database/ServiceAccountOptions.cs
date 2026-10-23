using CustomAction.CustomActionLogic.Interfaces.Database;
using WixToolset.Dtf.WindowsInstaller;

namespace CustomAction.CustomActionLogic.Database;
internal class ServiceAccountOptions : IServiceAccountOptions
{
    public ServiceAccountOptions(CustomActionData caData)
    {
        ServiceAccount = caData["ServiceAccount"];
    }
    public string ServiceAccount { get; }
}
