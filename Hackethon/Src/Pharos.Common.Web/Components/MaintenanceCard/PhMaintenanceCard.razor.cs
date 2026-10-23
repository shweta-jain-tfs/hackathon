using System.Globalization;
using Pharos.Common.Web.Components.Status;

namespace Pharos.Common.Web.Components.MaintenanceCard;
public partial class PhMaintenanceCard
{
    [Parameter] public string? Name { get; set; }
    [Parameter] public string? Description { get; set; }
    [Parameter] public EventCallback OnClick { get; set; }
    [Parameter] public string? Duration { get; set; }
    [Parameter] public DateTimeOffset? LastCompleted { get; set; }
    [Parameter] public DateTimeOffset? Next { get; set; }
    [Parameter] public string? ImageUrl { get; set; }
    [Parameter] public string? StatusText { get; set; }
    [Parameter] public MaintenanceCardStatus Status { get; set; } = MaintenanceCardStatus.None;
    [Parameter] public string? DurationLabel { get; set; }
    [Parameter] public string? LastCompletedLabel { get; set; }
    [Parameter] public string? NextLabel { get; set; }
    /// <summary>
    /// Optional culture to override the UI culture. Mostly for testing.
    /// </summary>
    [Parameter] public string? Culture { get; set; }

    private CultureInfo _culture = CultureInfo.CurrentCulture;

    private string LastCompletedDate => ToDateString(LastCompleted);
    private string LastCompletedTime => ToTimeString(LastCompleted);
    private string NextDate => ToDateString(Next);
    private string NextTime => ToTimeString(Next);
    private StatusType StatusIcon { get; set; } = StatusType.None;

    protected override void OnParametersSet()
    {
        if (!string.IsNullOrWhiteSpace(Culture))
        {
            try
            {
                _culture = CultureInfo.GetCultureInfo(Culture);
            }
            catch (CultureNotFoundException)
            {
                _culture = CultureInfo.CurrentCulture;
            }
        }
        else
        {
            _culture = CultureInfo.CurrentCulture;
        }

        StatusIcon = Status switch
        {
            MaintenanceCardStatus.Info => StatusType.Info,
            MaintenanceCardStatus.Warning => StatusType.Warning,
            MaintenanceCardStatus.Error => StatusType.Alarm,
            MaintenanceCardStatus.None or _ => StatusType.None,
        };
    }

    private string ToDateString(DateTimeOffset? dateTime)
    {
        if (dateTime is null || dateTime == DateTimeOffset.MinValue)
        {
            return "-";
        }

        return dateTime.Value.ToLocalTime().ToString("d", _culture);
    }

    private string ToTimeString(DateTimeOffset? dateTime)
    {
        if (dateTime is null || dateTime == DateTimeOffset.MinValue)
        {
            return "";
        }

        return dateTime.Value.ToLocalTime().ToString("t", _culture);
    }

}