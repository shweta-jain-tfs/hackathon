using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.Maintenance.Models;
using Pharos.Client.App.Maintenance.ViewModels;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Maintenance.Views;

public partial class MaintenancePage : IDisposable
{
    [Inject] private MaintenancePageViewModel ViewModel { get; set; } = null!;
    [Inject] private IModalService ModalService { get; set; } = null!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private IHelpContentService HelpContentService { get; set; } = null!;

    [SupplyParameterFromQuery]
    private int? ShowMaintenance { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await ViewModel.LoadAsync();
        ScheduledActivities = ViewModel.Where(x => x.ScheduledType is MaintenanceScheduledType.Scheduled).AsQueryable();
        OnDemandActivities = ViewModel.Where(x => x.ScheduledType is MaintenanceScheduledType.OnDemand or MaintenanceScheduledType.Both).AsQueryable();

        if (ShowMaintenance.HasValue && Enum.IsDefined(typeof(MaintenanceActivityType), ShowMaintenance.Value))
        {
            var activityType = (MaintenanceActivityType)ShowMaintenance.Value;
            var activity = ScheduledActivities.FirstOrDefault(x => x.Model.ActivityType == activityType);
            if (activity != null)
            {
                await ShowActivity(activity);
            }
        }
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        ViewModel.ViewRefreshNeeded += ViewModel_ViewRefreshNeeded;
    }

    private void ViewModel_ViewRefreshNeeded(object? sender, EventArgs e)
    {
        StateHasChanged();
    }

    private IQueryable<MaintenanceActivityViewModel> ScheduledActivities { get; set; } = Enumerable.Empty<MaintenanceActivityViewModel>().AsQueryable();

    private IQueryable<MaintenanceActivityViewModel> OnDemandActivities { get; set; } = Enumerable.Empty<MaintenanceActivityViewModel>().AsQueryable();

    private async Task ShowActivity(MaintenanceActivityViewModel activity)
    {
        var options = new ModalOptions
        {
            ModalHeight = ModalSize.XLarge,
            ModalWidth = ModalSize.XLarge,
            Status = ModalStatus.Default,
            Title = activity.DisplayName
        };

        var activities = await HelpContentService.GetMaintenanceActivities().ConfigureAwait(false);

        var helpContent = new MaintenanceHelpContent(activities);

        var activityGuideViewModel = new MaintenanceActivityGuideViewModel(activity.Model, ViewModel.MaintenanceService, ViewModel.StepFactory, helpContent);

        var parameters = new ModalParameters
        {
            {nameof(MaintenanceActivityGuideView.ViewModel), activityGuideViewModel}
        };

        var modal = ModalService.Show<MaintenanceActivityGuideView>(options, parameters);

        await activityGuideViewModel.ActivateFirstStep();

        var result = await modal.Result;

        if (result.Canceled && activityGuideViewModel.HasStartedActivity)
        {
            await ViewModel.MaintenanceService.AbortMaintenanceActivity(activity.Model);
        }
    }

    private static string GetImageUrl(MaintenanceActivityViewModel activity)
    {
        return $"_content/Pharos.Client.App.Maintenance/Images/Maintenance_{activity.ActivityType}.svg";
    }

    public void Dispose()
    {
        ViewModel.ViewRefreshNeeded -= ViewModel_ViewRefreshNeeded;
        ViewModel.Dispose();
        GC.SuppressFinalize(this);
    }
}