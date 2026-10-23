using Pharos.Client.App.Maintenance.Models;
using Pharos.Client.Common.Models;

namespace Pharos.Client.App.Maintenance.ViewModels;

public class MaintenanceActivityGuideViewModel
{
    public MaintenanceActivityGuideViewModel(MaintenanceActivity maintenanceActivity, IMaintenanceService maintenanceService, IActivityStepFactory stepFactory, IMaintenanceHelpContent helpContent)
    {
        Groups = stepFactory.CreateGroups(maintenanceActivity, maintenanceService, helpContent);
        Steps = GetSteps(Groups).ToList();
        SelectedStep = Steps.First();
    }

    public List<NavigationItem> Groups { get; }

    public List<ActivityStep> Steps { get; }

    public ActivityStep SelectedStep { get; private set; }

    public bool HasStartedActivity { get; private set; }

    private static IEnumerable<ActivityStep> GetSteps(List<NavigationItem> groups)
    {
        foreach (var item in groups)
        {
            if (item is StepGroup group)
            {
                foreach (var activityStep in group.Steps)
                {
                    yield return activityStep;
                }
            }

            if (item is ActivityStep step)
            {
                yield return step;
            }
        }
    }

    public void SelectStep(ActivityStep step)
    {
        SelectedStep = step;
    }

    public async Task<bool> HandleAction(StepAction action)
    {
        switch (action.ActionType)
        {
            case StepActionType.Start:
                var startOk = await StartAction(action);
                if (startOk)
                {
                    await NavigateNext();
                }

                break;
            case StepActionType.Next:
                await CallAction(action);
                await NavigateNext();
                break;
            case StepActionType.Previous:
                NavigatePrevious();
                break;
            case StepActionType.Done:
                return await DoneAction(action);
            case StepActionType.Performed:
                await CallAction(action);
                await NavigateNext();
                break;
            case StepActionType.ApiCall:
                await CallAction(action);
                break;
        }

        return true;
    }

    private async Task<bool> StartAction(StepAction action)
    {
        action.Enabled = false;

        Steps.ForEach(x =>
        {
            x.MenuNavigationEnabled = false;
        });

        SelectedStep.MenuNavigationEnabled = true;

        SelectedStep.SetMessage(string.Empty);

        var success = await action.Action();

        HasStartedActivity = success;

        if (!success)
        {
            action.Enabled = true;

            Steps.ForEach(x =>
            {
                x.MenuNavigationEnabled = true;
            });

            SelectedStep.SetMessage("Failed to start the activity. Please try again.");
        }

        return success;
    }

    private async Task NavigateNext()
    {
        var index = Steps.IndexOf(SelectedStep);
        if (index < Steps.Count - 1)
        {
            var nextStep = Steps[index + 1];
            nextStep.MenuNavigationEnabled = true;

            SelectedStep.Done = true;

            SelectStep(nextStep);

            await nextStep.Activate();
        }
    }

    private void NavigatePrevious()
    {
        var index = Steps.IndexOf(SelectedStep);
        if (index > 0)
        {
            SelectStep(Steps[index - 1]);
        }
    }

    private static async Task<bool> DoneAction(StepAction action)
    {
        return await CallAction(action);
    }

    private static async Task<bool> CallAction(StepAction action)
    {
        action.Enabled = false;

        var success = await action.Action();
        if (!success)
        {
            action.Enabled = true;
        }

        return success;
    }

    public async Task ActivateFirstStep()
    {
        await SelectedStep.Activate();
    }
}