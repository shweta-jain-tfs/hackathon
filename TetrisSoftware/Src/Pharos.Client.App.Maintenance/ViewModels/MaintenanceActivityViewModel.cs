using Pharos.Client.Common.Models;
using Pharos.Client.Common.ViewModels;
using Pharos.Common.Web.Components.MaintenanceCard;

namespace Pharos.Client.App.Maintenance.ViewModels;

public class MaintenanceActivityViewModel : ViewModelItem<MaintenanceActivity>
{
    public MaintenanceActivityViewModel(MaintenanceActivity model) : base(model)
    {
        Status = model.Status switch
        {
            MaintenanceActivityStatus.Overdue => MaintenanceCardStatus.Error,
            MaintenanceActivityStatus.DueSoon => MaintenanceCardStatus.Warning,
            MaintenanceActivityStatus.DueLater => MaintenanceCardStatus.Info,
            MaintenanceActivityStatus.None or _ => MaintenanceCardStatus.None
        };
    }

    public string Name => Model.ActivityType.ToString();
    public int Duration => Model.Duration;
    public DateTimeOffset LastCompleted => Model.LastCompleted.ToLocalTime();
    public MaintenanceScheduledType ScheduledType => Model.ScheduledType;
    public DateTimeOffset? Next => Model.Next;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string? DurationString { get; set; }

    public string? StatusText { get; set; }

    public MaintenanceCardStatus Status { get; set; } = MaintenanceCardStatus.None;

    public MaintenanceActivityType ActivityType => Model.ActivityType;
}