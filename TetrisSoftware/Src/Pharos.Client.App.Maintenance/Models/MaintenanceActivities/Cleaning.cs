using Pharos.Client.Common.Models;

namespace Pharos.Client.App.Maintenance.Models.MaintenanceActivities;

public class Cleaning : BaseActivity
{
    public static List<NavigationItem> CreateGroups(MaintenanceActivity maintenanceActivity, IMaintenanceService maintenanceService, IMaintenanceHelpContent helpContent)
    {
        var steps = new List<ActivityStep>();

        steps.Add(Step(helpContent.GetTranslation(MaintenanceActivityType.Cleaning, "0"), [Next()]));
        steps.Add(Step(helpContent.GetTranslation(MaintenanceActivityType.Cleaning, "1"), [Previous(), Next()]));
        steps.Add(Step(helpContent.GetTranslation(MaintenanceActivityType.Cleaning, "2"), [Previous(), Next()]));
        steps.Add(Step(helpContent.GetTranslation(MaintenanceActivityType.Cleaning, "3"), [Previous(), Next()]));

        // the restore step need to listen to changes in the bottle state
        var restoreBottle = Step(helpContent.GetTranslation(MaintenanceActivityType.Cleaning, "4"), [Previous(), Next(false)]);
        restoreBottle.SetActivate(async () => await CheckBottle(restoreBottle, maintenanceService));
        steps.Add(restoreBottle);

        var startAction = new StepAction
        {
            ActionType = StepActionType.Done, // has to be done to close the modal
            Enabled = false,
            Text = "Start maintenance process now",
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
        var startStep = Step(helpContent.GetTranslation(MaintenanceActivityType.Cleaning, "5"), [Previous(), startAction]);
        startStep.SetActivate(async () => await CheckThatMaintenanceCanBeStarted(startStep, maintenanceService));
        steps.Add(startStep);

        return CreateGroup(steps);
    }
    
    private static async Task CheckBottle(ActivityStep restoreBottle, IMaintenanceService maintenanceService)
    {
        var refreshAction = restoreBottle.Actions.FirstOrDefault(x => x.ActionType == StepActionType.ApiCall);
        if (refreshAction != null)
        {
            restoreBottle.Actions.Remove(refreshAction);
        }

        restoreBottle.SetNamedMessages([]);
        restoreBottle.SetMessage("Checking bottle state...");

        var bottleState = await maintenanceService.GetMaintenanceBottleState();

        restoreBottle.SetMessage(string.Empty);
        restoreBottle.SetNamedMessages([("Is in position", bottleState.IsInPosition.ToString()), ("Liquid level ok", bottleState.IsLiquidLevelOk.ToString())]);

        if (bottleState is { IsInPosition: true, IsLiquidLevelOk: true })
        {
            EnableActionTypes(restoreBottle.Actions, StepActionType.Next);
        }
        else
        {
            refreshAction = StepAction("Refresh", StepActionType.ApiCall);
            refreshAction.Action = async () =>
            {
                await CheckBottle(restoreBottle, maintenanceService);
                return true;
            };
            restoreBottle.Actions.Add(refreshAction);
        }
    }
}