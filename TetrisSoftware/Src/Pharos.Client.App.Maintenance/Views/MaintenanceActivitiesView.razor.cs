using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.Maintenance.ViewModels;
using Pharos.Client.Common.Localization;

namespace Pharos.Client.App.Maintenance.Views;

public partial class MaintenanceActivitiesView
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    [Parameter] public IQueryable<MaintenanceActivityViewModel>? ViewModels { get; set; }
    [Parameter] public EventCallback<MaintenanceActivityViewModel> OnShowActivity { get; set; }

    private async Task ViewMaintenanceActivity(MaintenanceActivityViewModel activity)
    {
        await OnShowActivity.InvokeAsync(activity);
    }
}