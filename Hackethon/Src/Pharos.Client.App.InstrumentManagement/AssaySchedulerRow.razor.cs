using Microsoft.AspNetCore.Components;

namespace Pharos.Client.App.InstrumentManagement;

public partial class AssaySchedulerRow
{
    [Parameter] public string Name { get; set; } = string.Empty;
    [Parameter] public bool IsAuthorized { get; set; }
    [Parameter] public bool IsActive { get; set; }
    [Parameter] public bool VisibleSwitch { get; set; }
    [Parameter] public EventCallback<bool> IsActiveChanged { get; set; }
    [Parameter] public EventCallback OnLinkClick { get; set; }
}
