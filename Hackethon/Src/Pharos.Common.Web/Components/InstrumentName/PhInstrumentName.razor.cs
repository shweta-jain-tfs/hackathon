using Pharos.Common.Web.Components.Health;
using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.InstrumentName;

/// <summary>
/// Displays the name of an instrument along with its health status and an optional link. For use in the status bar.
/// </summary>
public partial class PhInstrumentName
{
    [Parameter] public string? Name { get; set; }
    [Parameter] public HealthStatus Status { get; set; }
    [Parameter] public string? Href { get; set; }
    [Parameter] public bool LockScreenMode { get; set; }

    private string CssClasses =>
        new CssBuilder("ph-instrument-name")
            .AddClass("ph-instrument-name--lock-screen", LockScreenMode)
            .Build();

    private string CssClassesStatus =>
       new CssBuilder("ph-instrument-name__status")
       .AddClass("ph-instrument-name__status--white-outline", LockScreenMode && Status == HealthStatus.Ok)
       .AddClass("ph-instrument-name__status--white-outline", LockScreenMode && Status == HealthStatus.Alarm)
       .Build();
}