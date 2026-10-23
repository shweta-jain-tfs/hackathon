using Pharos.Client.Common.Models;

namespace Pharos.Client.App.Maintenance.Models.MaintenanceActivities;

public class ExtendedRinse : BaseActivity
{
    public static List<NavigationItem> CreateGroups(MaintenanceActivity maintenanceActivity, IMaintenanceService maintenanceService, IMaintenanceHelpContent helpContent)
    {
        var steps = new List<ActivityStep>();

        steps.Add(Step(helpContent.GetTranslation(MaintenanceActivityType.ExtendedRinse, "0"), [Next()]));
        steps.Add(Step(helpContent.GetTranslation(MaintenanceActivityType.ExtendedRinse, "1"), [Previous(), Next()]));
        steps.Add(Step(helpContent.GetTranslation(MaintenanceActivityType.ExtendedRinse, "2"), [Previous(), Next()]));

        var startAction = new StepAction
        {
            ActionType = StepActionType.Done, // has to be done to close the modal
            Enabled = false,
            Text = "Start extended rinse",
            Action = async () =>
            {
                var result = await maintenanceService.StartMaintenanceActivity(maintenanceActivity);
                if (result.StartMaintenance == StartMaintenance.MaintenanceStartOk)
                {
                    return true;
                }

                return false;
            }
        };
        var startStep = Step(helpContent.GetTranslation(MaintenanceActivityType.ExtendedRinse, "3"), [Previous(), startAction]);
        startStep.SetActivate(async () => await CheckThatMaintenanceCanBeStarted(startStep, maintenanceService));
        steps.Add(startStep);

        return CreateGroup(steps);
    }
}