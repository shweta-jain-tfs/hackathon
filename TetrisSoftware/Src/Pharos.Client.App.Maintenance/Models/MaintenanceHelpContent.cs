using Pharos.Client.Common.Models;
using Pharos.Client.Common.Models.HelpContent;

namespace Pharos.Client.App.Maintenance.Models;

public interface IMaintenanceHelpContent
{
    HelpContentMaintenanceActivity GetActivity(MaintenanceActivityType activityType);
    StepTranslations GetTranslation(MaintenanceActivityType activityType, string stepKey);
}

public class MaintenanceHelpContent : IMaintenanceHelpContent
{
    public MaintenanceHelpContent(List<HelpContentMaintenanceActivity> activities)
    {
        _activities = activities;
    }

    private readonly List<HelpContentMaintenanceActivity> _activities;

    public HelpContentMaintenanceActivity GetActivity(MaintenanceActivityType activityType)
    {
        return _activities.FirstOrDefault(x => x.Key == activityType.ToString())!;
    }

    public StepTranslations GetTranslation(MaintenanceActivityType activityType, string stepKey)
    {
        var activity = GetActivity(activityType);
        var step = activity.Steps.FirstOrDefault(x => x.Key == stepKey);
        return step != null ? new StepTranslations(step.NavigationTitle, step.Title ?? string.Empty, step.Content ?? string.Empty) : new StepTranslations("Lost in translation", "Lost in translation", "Lost in translation");
    }
}

public record StepTranslations(string NavigationName, string Title, string Content);