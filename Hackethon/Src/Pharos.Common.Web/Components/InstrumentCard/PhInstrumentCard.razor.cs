using Pharos.Common.Web.Components.Health;
using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.InstrumentCard;

/// <summary>
/// Card component for displaying an instrument and its status in instrument lists.
/// Supports instrument actions and optional scheduler information.
/// </summary>
public enum AssaySchedulerIconType
{
    None,
    Sun,
    Moon
}

public partial class PhInstrumentCard
{
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public bool AssaySchedulerShowInfo { get; set; }
    [Parameter] public bool AssaySchedulerIsActive { get; set; }
    [Parameter] public bool AssaySchedulerUserIsAuthorized { get; set; }
    [Parameter] public string? AssaySchedulerTextBeforeIcon { get; set; }
    [Parameter] public string? AssaySchedulerTextAfterIcon { get; set; }
    [Parameter] public AssaySchedulerIconType AssaySchedulerIcon { get; set; }
    [Parameter] public EventCallback<bool> AssaySchedulerIsActiveChanged { get; set; }

    [Parameter] public bool Enabled { get; set; }
    [Parameter] public HealthStatus HealthStatus { get; set; }
    [Parameter] public string? RunInformationRow1 { get; set; }
    [Parameter] public string? RunInformationRow2 { get; set; }
    [Parameter] public IReadOnlyList<string> Technologies { get; set; } = [];
    [Parameter] public string? TechnologiesLabel { get; set; }
    [Parameter] public string? Title { get; set; }
    [Parameter] public bool Value { get; set; }
    [Parameter] public string? DeleteActionLabel { get; set; }
    [Parameter] public string? DisableActionLabel { get; set; }
    [Parameter] public string? EnableActionLabel { get; set; }
    [Parameter] public string? RenameActionLabel { get; set; }
    [Parameter] public string? SelectTechnologiesActionLabel { get; set; }
    [Parameter] public EventCallback OnClickedDeleteEvent { get; set; }
    [Parameter] public EventCallback OnClickedDisableEvent { get; set; }
    [Parameter] public EventCallback OnClickedEnableEvent { get; set; }
    [Parameter] public EventCallback OnClickedRenameEvent { get; set; }
    [Parameter] public EventCallback OnClickedSelectTechnologiesEvent { get; set; }
    [Parameter] public bool IsConnected { get; set; }
    [Parameter] public bool InstrumentActionsUserIsAuthorized { get; set; } = true;

    /// <summary>Whether this card is currently selected.</summary>
    [Parameter] public bool IsSelected { get; set; }

    /// <summary>Raised when the card body is clicked (for selection toggling).</summary>
    [Parameter] public EventCallback OnCardClicked { get; set; }

    private string CardCssClass =>
        IsSelected
            ? "ph-instrument-card ph-instrument-card--selected"
            : "ph-instrument-card";

    private string SchedulingIconCssClasses =>
        new CssBuilder("ph-instrument-card__scheduling-icon")
            .AddClass("ph-instrument-card__scheduling-icon--hidden", AssaySchedulerIcon == AssaySchedulerIconType.None)
            .Build();

    private async Task CardClicked()
    {
        await OnCardClicked.InvokeAsync();
    }

    private async Task OnAssaySchedulerSwitchChanged(bool newValue)
    {
        await AssaySchedulerIsActiveChanged.InvokeAsync(newValue);
    }

    private async Task OnClickedDelete()
    {
        await OnClickedDeleteEvent.InvokeAsync();
    }

    private async Task OnClickedDisable()
    {
        await OnClickedDisableEvent.InvokeAsync();
    }

    private async Task OnClickedEnable()
    {
        await OnClickedEnableEvent.InvokeAsync();
    }

    private async Task OnClickedRename()
    {
        await OnClickedRenameEvent.InvokeAsync();
    }

    private async Task OnClickedSelectTechnologies()
    {
        await OnClickedSelectTechnologiesEvent.InvokeAsync();
    }
}
