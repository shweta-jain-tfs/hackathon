using Pharos.Client.Common.Models;

namespace Pharos.Client.App.Maintenance.Models.MaintenanceActivities;

public class ManualCleaning : BaseActivity
{
    public static List<NavigationItem> CreateGroups(MaintenanceActivity maintenanceActivity, IMaintenanceService maintenanceService, IMaintenanceHelpContent helpContent)
    {
        var groups = new List<NavigationItem>();

        var pipetteCleaningHelpContent = helpContent.GetActivity(MaintenanceActivityType.PipetteCleaning);
        var pipetteCleaningSteps = PipetteCleaning.CreateSteps(maintenanceActivity, maintenanceService, helpContent);
        var gr1 = new StepGroup(pipetteCleaningHelpContent.Title, pipetteCleaningSteps);
        ChangeDoneAndPerformedToPerformed(pipetteCleaningSteps, MaintenanceActivityType.PipetteCleaning, maintenanceService, gr1);
        groups.AddRange(gr1);

        var cleanRinseContainerHelpContent = helpContent.GetActivity(MaintenanceActivityType.CleanRinseContainer);
        var cleanRinseContainerSteps = CleanRinseContainer.CreateSteps(maintenanceActivity, maintenanceService, helpContent);
        var gr2 = new StepGroup(cleanRinseContainerHelpContent.Title, cleanRinseContainerSteps);
        ChangeDoneAndPerformedToPerformed(cleanRinseContainerSteps, MaintenanceActivityType.CleanRinseContainer, maintenanceService, gr2);
        groups.AddRange(gr2);

        var cleanSolidWasteBinsHelpContent = helpContent.GetActivity(MaintenanceActivityType.CleanSolidWasteBins);
        var cleanSolidWasteBinsSteps = CleanSolidWasteBins.CreateSteps(maintenanceActivity, maintenanceService, helpContent);
        var gr3 = new StepGroup(cleanSolidWasteBinsHelpContent.Title, cleanSolidWasteBinsSteps);
        ChangeDoneAndPerformedToPerformed(cleanSolidWasteBinsSteps, MaintenanceActivityType.CleanSolidWasteBins, maintenanceService, gr3);
        groups.AddRange(gr3);

        //Add done step for ManualCleaning
        groups.Add(Step(helpContent.GetTranslation(MaintenanceActivityType.ManualCleaning, "0"), [Previous(false), DoneAndPerformed("Finish", maintenanceActivity, maintenanceService)], EnableAllActionTypes));

        return groups;
    }

    private static void ChangeDoneAndPerformedToPerformed(List<ActivityStep> activitySteps, MaintenanceActivityType maintenanceActivityType, IMaintenanceService maintenanceService, StepGroup stepGroup)
    {
        foreach (var activityStep in activitySteps)
        {
            foreach (var stepAction in activityStep.Actions.ToList())
            {
                if (stepAction.ActionType == StepActionType.Done)
                {
                    activityStep.Actions.Remove(stepAction);

                    var performed = Performed(stepAction.Text, maintenanceActivityType, maintenanceService, stepGroup);
                    activityStep.Actions.Add(performed);
                }
            }
        }
    }
}