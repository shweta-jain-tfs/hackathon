using Pharos.Client.App.Maintenance.Models.MaintenanceActivities;
using Pharos.Client.Common.Models;

namespace Pharos.Client.App.Maintenance.Models;

public interface IActivityStepFactory
{
    List<NavigationItem> CreateGroups(MaintenanceActivity maintenanceActivity, IMaintenanceService maintenanceService, IMaintenanceHelpContent helpContent);
}

public class ActivityStepFactory : IActivityStepFactory
{
    public List<NavigationItem> CreateGroups(MaintenanceActivity maintenanceActivity, IMaintenanceService maintenanceService, IMaintenanceHelpContent helpContent)
    {
        return maintenanceActivity.ActivityType switch
        {
            MaintenanceActivityType.Cleaning => Cleaning.CreateGroups(maintenanceActivity, maintenanceService, helpContent),
            MaintenanceActivityType.ManualCleaning => ManualCleaning.CreateGroups(maintenanceActivity, maintenanceService, helpContent),
            MaintenanceActivityType.ExtendedRinse => ExtendedRinse.CreateGroups(maintenanceActivity, maintenanceService, helpContent),
            MaintenanceActivityType.Rinse => Rinse.CreateGroups(maintenanceActivity, maintenanceService, helpContent),
            MaintenanceActivityType.PipetteCleaning => PipetteCleaning.CreateGroups(maintenanceActivity, maintenanceService, helpContent),
            MaintenanceActivityType.CleanWasteChute => CleanWasteChute.CreateGroups(maintenanceActivity, maintenanceService, helpContent),
            MaintenanceActivityType.CleanLiquidWasteContainer => CleanLiquidWasteContainer.CreateGroups(maintenanceActivity, maintenanceService, helpContent),
            MaintenanceActivityType.CleanRinseContainer => CleanRinseContainer.CreateGroups(maintenanceActivity, maintenanceService, helpContent),
            MaintenanceActivityType.CleanSolidWasteBins => CleanSolidWasteBins.CreateGroups(maintenanceActivity, maintenanceService, helpContent),
            MaintenanceActivityType.CleanWashStations => CleanWashStations.CreateGroups(maintenanceActivity, maintenanceService, helpContent),
            MaintenanceActivityType.CleanTransferUnitTips => CleanTransferUnitTips.CreateGroups(maintenanceActivity, maintenanceService, helpContent),
            MaintenanceActivityType.CleanEjectUnitTips => CleanEjectUnitTips.CreateGroups(maintenanceActivity, maintenanceService, helpContent),
            MaintenanceActivityType.InspectDispenseUnit => InspectDispenseUnit.CreateGroups(maintenanceActivity, maintenanceService, helpContent),
            MaintenanceActivityType.InspectCapWellRemovers => InspectCapWellRemovers.CreateGroups(maintenanceActivity, maintenanceService, helpContent),
            _ => throw new ArgumentOutOfRangeException(nameof(maintenanceActivity), maintenanceActivity.ActivityType.ToString(), "Failed to create steps for the maintenence activity")
        };
    }
}