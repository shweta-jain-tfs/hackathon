using Pharos.Client.Common.Models;

namespace Pharos.Client.App.Maintenance.Models.MaintenanceActivities;

public class BaseActivity
{
    protected static ActivityStep Step(string navigationName, string title, string content, List<StepAction> actions, Action<ActivityStep>? activate = null)
    {
        var step = new ActivityStep(navigationName)
        {
            Text = content,
            Title = title
        };

        step.Actions.AddRange(actions);

        if (activate != null)
        {
            step.SetActivate(() =>
            {
                activate(step);
                return Task.CompletedTask;
            });
        }

        return step;
    }
    protected static ActivityStep Step(StepTranslations translations, List<StepAction> actions, Action<ActivityStep>? activate = null)
    {
        return Step(translations.NavigationName, translations.Title, translations.Content, actions, activate);
    }

    protected static ActivityStep Step(StepTranslations translations)
    {
        return Step(translations.NavigationName, translations.Title, translations.Content, [], null);
    }

    protected static void EnableAllActionTypes(List<StepAction> actions)
    {
        actions.ForEach(x => x.Enabled = true);
    }

    protected static void EnableAllActionTypes(ActivityStep step)
    {
        EnableAllActionTypes(step.Actions);
    }

    protected static void EnableActionTypes(List<StepAction> actions, params StepActionType[] actionTypes)
    {
        foreach (var stepAction in actions.Where(x => actionTypes.Contains(x.ActionType)))
        {
            stepAction.Enabled = true;
        }
    }

    protected static void EnableActionTypes(ActivityStep step, params StepActionType[] actionTypes)
    {
        EnableActionTypes(step.Actions, actionTypes);
    }

    protected static List<NavigationItem> CreateGroup(List<ActivityStep> steps)
    {
        return [new StepGroup(string.Empty, steps)];
    }

    protected static StepAction Next(bool enabled = true)
    {
        return StepAction("Next", StepActionType.Next, enabled);
    }

    protected static StepAction Previous(bool enabled = true)
    {
        return StepAction("Previous", StepActionType.Previous, enabled);
    }

    protected static StepAction Start(string text, MaintenanceActivity maintenanceActivity, IMaintenanceService maintenanceService)
    {
        var stepAction = StepAction(text, StepActionType.Start, false);
        stepAction.Action = async () =>
        {
            var result = await maintenanceService.StartMaintenanceActivity(maintenanceActivity);
            return result.StartMaintenance == StartMaintenance.MaintenanceStartOk;
        };
        return stepAction;
    }

    protected static StepAction Done(string text, MaintenanceActivity maintenanceActivity, IMaintenanceService maintenanceService)
    {
        var stepAction = StepAction(text, StepActionType.Done, false);
        stepAction.Action = async () =>
        {
            await maintenanceService.MaintenanceActivityDone(maintenanceActivity);
            return true;
        };
        return stepAction;
    }

    protected static StepAction Performed(string text, MaintenanceActivityType activityType, IMaintenanceService maintenanceService, StepGroup stepGroup)
    {
        var stepAction = StepAction(text, StepActionType.Next, false);
        stepAction.Action = async () =>
        {
            await maintenanceService.MaintenanceActivityPerformed(activityType);
            stepGroup.Done = true;
            return true;
        };
        return stepAction;
    }

    protected static StepAction DoneAndPerformed(string text, MaintenanceActivity maintenanceActivity, IMaintenanceService maintenanceService)
    {
        var stepAction = StepAction(text, StepActionType.Done, false);
        stepAction.Action = async () =>
        {
            await maintenanceService.MaintenanceActivityDone(maintenanceActivity);
            await maintenanceService.MaintenanceActivityPerformed(maintenanceActivity.ActivityType);
            return true;
        };
        return stepAction;
    }

    protected static StepAction StepAction(string text, StepActionType actionType, bool enabled = true)
    {
        return new StepAction
        {
            Enabled = enabled,
            ActionType = actionType,
            Text = text
        };
    }

    protected static async Task CheckThatMaintenanceCanBeStarted(ActivityStep startStep, IMaintenanceService maintenanceService)
    {
        var refreshAction = startStep.Actions.FirstOrDefault(x => x.ActionType == StepActionType.ApiCall);
        if (refreshAction != null)
        {
            startStep.Actions.Remove(refreshAction);
        }

        startStep.SetMessage("Checking instrument state...");

        var maintenanceInfo = await maintenanceService.CanStartMaintenance();

        if (maintenanceInfo.CanStartMaintenance)
        {
            EnableActionTypes(startStep.Actions, StepActionType.Start, StepActionType.Done);
            startStep.SetMessage(string.Empty);
        }
        else
        {
            refreshAction = StepAction("Refresh", StepActionType.ApiCall);
            refreshAction.Action = async () =>
            {
                await CheckThatMaintenanceCanBeStarted(startStep, maintenanceService);
                return true;
            };
            startStep.Actions.Add(refreshAction);
            startStep.SetMessage("Cannot start at the moment");
        }
    }

    protected static async Task CheckThatMaintenanceCanBeStartedChangeToNext(ActivityStep startStep, IMaintenanceService maintenanceService)
    {
        var refreshAction = startStep.Actions.FirstOrDefault(x => x.ActionType == StepActionType.ApiCall);
        if (refreshAction != null)
        {
            startStep.Actions.Remove(refreshAction);
        }

        startStep.SetMessage("Checking instrument state...");

        var maintenanceInfo = await maintenanceService.CanStartMaintenance();

        //if the instrument is in maintenance mode, we change the start action to next action
        if (maintenanceInfo.IsInMaintenanceMode)
        {
            ChangeStartToNext(startStep);
            startStep.SetMessage(string.Empty);
        }
        else
        {
            if (maintenanceInfo.CanStartMaintenance)
            {
                EnableActionTypes(startStep.Actions, StepActionType.Start, StepActionType.Done);
                startStep.SetMessage(string.Empty);
            }
            else
            {
                refreshAction = StepAction("Refresh", StepActionType.ApiCall);
                refreshAction.Action = async () =>
                {
                    await CheckThatMaintenanceCanBeStartedChangeToNext(startStep, maintenanceService);
                    return true;
                };
                startStep.Actions.Add(refreshAction);
                startStep.SetMessage("Cannot start at the moment");
            }
        }
    }

    private static void ChangeStartToNext(ActivityStep activityStep)
    {
        foreach (var stepAction in activityStep.Actions.ToList())
        {
            if (stepAction.ActionType == StepActionType.Start)
            {
                activityStep.Actions.Remove(stepAction);

                var next = Next();
                activityStep.Actions.Add(next);
            }
        }
    }
}