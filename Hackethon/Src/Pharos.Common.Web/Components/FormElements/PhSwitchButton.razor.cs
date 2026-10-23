using System.Linq.Expressions;
using Pharos.Common.Web.Components.FormElements.Enums;
using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.FormElements;

/// <summary>
/// A toggle switch button for boolean on/off values.
/// </summary>
public partial class PhSwitchButton
{
    [Parameter] public string? Id { get; set; }
    [Parameter] public string? Label { get; set; }
    [Parameter] public SwitchButtonLabelPosition LabelPosition { get; set; } = SwitchButtonLabelPosition.Left;
    [Parameter] public SwitchButtonSize Size { get; set; } = SwitchButtonSize.Small;

    /// <summary>
    /// Renders the component as display-only (non-interactive, no automatic validation).
    /// Overrides <see cref="Disabled"/>, but keeps the disabled label and value color.
    /// </summary>
    [Parameter] public bool DisplayOnly { get; set; }
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public bool Value { get; set; }
    [Parameter] public EventCallback<bool> ValueChanged { get; set; }
    [Parameter] public Expression<Func<bool>>? ValueExpression { get; set; }
    [Parameter] public EventCallback<bool> OnCheckChanged { get; set; }

    private bool _enableAnimation;
    private bool IsOn => Value;

    protected string Classname =>
        new CssBuilder("ph-switchbutton")
            .AddClass("ph-switchbutton--on", IsOn)
            .AddClass("ph-switchbutton--disabled", Disabled)
            .AddClass("ph-switchbutton--display-only", DisplayOnly)
            .AddClass("ph-switchbutton--label-right", LabelPosition == SwitchButtonLabelPosition.Right)
            .AddClass("ph-switchbutton--large", Size == SwitchButtonSize.Large)
            .AddClass("ph-switchbutton--animate", _enableAnimation)
            .Build();

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _enableAnimation = true;
            StateHasChanged();
        }
    }

    private async Task SetCurrentValueAsync(bool value)
    {
        if (ValueChanged.HasDelegate)
        {
            await ValueChanged.InvokeAsync(value);
        }

        if (OnCheckChanged.HasDelegate)
        {
            await OnCheckChanged.InvokeAsync(value);
        }
    }

    private async Task OnInternalChange(ChangeEventArgs _)
    {
        if (DisplayOnly)
        {
            return;
        }

        await SetCurrentValueAsync(!Value);
    }
}