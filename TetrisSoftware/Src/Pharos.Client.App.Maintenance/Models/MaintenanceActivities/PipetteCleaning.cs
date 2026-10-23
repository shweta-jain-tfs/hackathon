using Pharos.Client.Common.Models;

namespace Pharos.Client.App.Maintenance.Models.MaintenanceActivities;

public class PipetteCleaning : BaseActivity
{
    public static List<ActivityStep> CreateSteps(MaintenanceActivity maintenanceActivity, IMaintenanceService maintenanceService, IMaintenanceHelpContent helpContent)
    {
        var steps = new List<ActivityStep>();

        var overviewStep = Step(helpContent.GetTranslation(MaintenanceActivityType.PipetteCleaning, "0"), [Next()]);
        
        var startAction = new StepAction
        {
            ActionType = StepActionType.Start,
            Enabled = false,
            Text = "Start maintenance activity",
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

        overviewStep.Actions.Add(startAction);

        overviewStep.SetActivate(async () => await CheckThatMaintenanceCanBeStartedChangeToNext(overviewStep, maintenanceService));

        steps.Add(overviewStep);

        steps.Add(Step(helpContent.GetTranslation(MaintenanceActivityType.PipetteCleaning, "1"), [Previous(false), Next(false)], EnableAllActionTypes));
        steps.Add(Step(helpContent.GetTranslation(MaintenanceActivityType.PipetteCleaning, "2"), [Previous(false), Next(false)], EnableAllActionTypes));
        steps.Add(Step(helpContent.GetTranslation(MaintenanceActivityType.PipetteCleaning, "3"), [Previous(false), DoneAndPerformed("Finish", maintenanceActivity, maintenanceService)], EnableAllActionTypes));

        return steps;
    }

    public static List<NavigationItem> CreateGroups(MaintenanceActivity maintenanceActivity, IMaintenanceService maintenanceService, IMaintenanceHelpContent helpContent)
    {
        return CreateGroup(CreateSteps(maintenanceActivity, maintenanceService, helpContent));
    }
}