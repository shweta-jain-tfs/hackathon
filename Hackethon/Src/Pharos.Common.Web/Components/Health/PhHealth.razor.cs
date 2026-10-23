using Pharos.Common.Web.Components.Icons;

namespace Pharos.Common.Web.Components.Health;

/// <summary>
/// Shows instrument health status. See <see cref="PhStatusIcon"/> for general status icons.
/// </summary>
public partial class PhHealth
{
    [Parameter] public HealthStatus Status { get; set; } = HealthStatus.Ok;
    [Parameter] public HealthSize Size { get; set; } = HealthSize.Md;
    [Parameter] public bool Outline { get; set; }


    private StatusIconSize IconSize => Size switch
    {
        HealthSize.Sm => StatusIconSize.Lg,
        HealthSize.Lg => StatusIconSize.Xxxxl,
        HealthSize.Md or _ => StatusIconSize.Xxl,
    };

    private StatusIconType Icon => Status switch
    {
        HealthStatus.Standby => StatusIconType.Standby,
        HealthStatus.Warning => StatusIconType.Warning,
        HealthStatus.Alarm => StatusIconType.Alarm,
        HealthStatus.Off => StatusIconType.Off,
        HealthStatus.Ok or _ => StatusIconType.Ok,
    };
}