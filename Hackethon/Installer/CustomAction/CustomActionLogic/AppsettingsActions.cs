using System;
using CustomAction.CustomActionLogic.Interfaces.AppSettings;
using CustomAction.Wrappers.Interfaces;
using WixToolset.Dtf.WindowsInstaller;

namespace CustomAction.CustomActionLogic;

public class AppSettingsActions(ISessionWrapper session, IAppSettingsUpdater appSettingsUpdater)
{
    public ActionResult UpdateAppSettings()
    {
        try
        {
            appSettingsUpdater.UpdateDataProtectionSetting();

            return ActionResult.Success;
        }
        catch (Exception e)
        {
            session.Log($"Exception: {e.Message}");
            session.Message(InstallMessage.Error, new Record { FormatString = e.Message });
            return ActionResult.Failure;
        }
    }
}