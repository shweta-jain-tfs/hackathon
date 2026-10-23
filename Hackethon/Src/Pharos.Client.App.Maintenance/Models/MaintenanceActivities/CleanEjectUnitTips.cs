using Pharos.Client.Common.Models;

namespace Pharos.Client.App.Maintenance.Models.MaintenanceActivities;

public class CleanEjectUnitTips : BaseActivity
{
    public static List<NavigationItem> CreateGroups(MaintenanceActivity maintenanceActivity, IMaintenanceService maintenanceService, IMaintenanceHelpContent helpContent)
    {
        var steps = new List<ActivityStep>();
        
        var overviewStep = Step(helpContent.GetTranslation(MaintenanceActivityType.CleanEjectUnitTips, "0"));
        var startAction = Start("Start maintenance activity", maintenanceActivity, maintenanceService);
        startAction.Enabled = true;
        overviewStep.Actions.Add(startAction);

        steps.Add(overviewStep);

        steps.Add(Step(helpContent.GetTranslation(MaintenanceActivityType.CleanEjectUnitTips, "1"), [Previous(false), Next(false)], EnableAllActionTypes));
        steps.Add(Step(helpContent.GetTranslation(MaintenanceActivityType.CleanEjectUnitTips, "2"), [Previous(false), Done("Finish", maintenanceActivity, maintenanceService)], EnableAllActionTypes));

        return CreateGroup(steps);
    }
}