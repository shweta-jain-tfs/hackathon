using System.Globalization;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.Maintenance.Models;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using Pharos.Client.Common.ViewModels;

namespace Pharos.Client.App.Maintenance.ViewModels;

public class MaintenancePageViewModel : ListViewModel<MaintenanceActivityViewModel, MaintenanceActivity>
{
    private readonly IStringLocalizer<Translations> _loc;

    public MaintenancePageViewModel(IMaintenanceService maintenanceService, IActivityStepFactory stepFactory, IMaintenanceItemsSource itemsSource, IMaintenanceRefreshNotificationProvider refreshNotificationProvider, IStringLocalizer<Translations> localizer) : base(itemsSource, refreshNotificationProvider)
    {
        MaintenanceService = maintenanceService;
        StepFactory = stepFactory;
        _loc = localizer;
    }

    public IMaintenanceService MaintenanceService { get; }

    public IActivityStepFactory StepFactory { get; }

    protected override MaintenanceActivityViewModel CreateViewModel(MaintenanceActivity model)
    {
        var viewModel = new MaintenanceActivityViewModel(model);

        viewModel.DisplayName = GetTextResourceOrDefault($"MaintenanceActivity_{viewModel.Name}", viewModel.Name);
        viewModel.DurationString = $"{viewModel.Duration} {_loc["MaintenancePage_DurationUnit"]}";
        if (model.ActivityType is MaintenanceActivityType.Cleaning or MaintenanceActivityType.ManualCleaning)
        {
            viewModel.Description = GetTextResourceOrDefault($"MaintenanceActivity_{viewModel.Name}_Description", "");
        }

        if (model.Status is MaintenanceActivityStatus.DueLater or MaintenanceActivityStatus.DueSoon or MaintenanceActivityStatus.Overdue)
        {
            viewModel.StatusText = GetActivityDueMessage(viewModel);
        }

        return viewModel;
    }

    private string? GetActivityDueMessage(MaintenanceActivityViewModel viewModel)
    {
        if (!viewModel.Next.HasValue)
        {
            return null;
        }

        var now = DateTimeOffset.Now;
        var timeSpan = viewModel.Next.Value - now;

        if (timeSpan.TotalDays >= 2)
        {
            var totalDays = (int)timeSpan.TotalDays;
            return _loc["MaintenancePage_DueInDays"].Value.Replace("{0}", totalDays.ToString(CultureInfo.InvariantCulture));
        }
        else if (timeSpan.TotalHours >= 2)
        {
            var totalHours = (int)timeSpan.TotalHours;
            return _loc["MaintenancePage_DueInHours"].Value.Replace("{0}", totalHours.ToString(CultureInfo.InvariantCulture));
        }
        else if (timeSpan.TotalMinutes >= 1)
        {
            var totalMinutes = (int)timeSpan.TotalMinutes;
            return _loc["MaintenancePage_DueInMinutes"].Value.Replace("{0}", totalMinutes.ToString(CultureInfo.InvariantCulture));
        }
        else
        {
            return _loc["MaintenancePage_DueNow"];
        }
    }

    private string GetTextResourceOrDefault(string key, string defaultString)
    {
        var localizedString = _loc.GetString(key);
        if (localizedString.ResourceNotFound)
        {
            return defaultString;
        }

        return localizedString.Value;
    }
}