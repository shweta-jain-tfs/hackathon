using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.FormElements.RadioButton;

/// <summary>
/// A single radio button option used inside a <see cref="PhRadioGroup{T}"/>.
/// </summary>
public partial class PhRadioButton<T>
{
    [CascadingParameter] private PhRadioGroup<T>? ParentGroup { get; set; }

    /// <summary>
    /// Additional content displayed below the radio button when this option is selected.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom label content for interactive mode. Overrides <see cref="Label"/>.
    /// <see cref="Label"/> is used when the radio button is display-only.
    /// </summary>
    [Parameter] public RenderFragment? LabelContent { get; set; }

    /// <summary>
    /// Disables the radio button.
    /// </summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>
    /// Label displayed next to the radio button.
    /// </summary>
    [Parameter] public string Label { get; set; } = "";

    /// <summary>
    /// The value represented by this radio button.
    /// </summary>
    [Parameter] public T? Value { get; set; }

    /// <summary>
    /// Optional help text shown below the label.
    /// </summary>
    [Parameter] public string? LabelHelpText { get; set; }

    private bool IsDisplayOnly => ParentGroup?.DisplayOnly == true;

    private bool IsChecked => ParentGroup != null && EqualityComparer<T>.Default.Equals(Value, ParentGroup.SelectedValue);
    private bool HasError => ParentGroup != null && ParentGroup.HasError && (IsChecked || ParentGroup.SelectedValue == null);

    protected string Classname => new CssBuilder("ph-radio-button")
        .AddClass("ph-radio-button--error", HasError)
        .AddClass("ph-radio-button--disabled", Disabled)
        .AddClass("ph-radio-button--display-only", IsDisplayOnly)
        .Build();

    private void OnSelected()
    {
        if (IsDisplayOnly || Disabled)
        {
            return;
        }

        ParentGroup?.UpdateSelectedValue(Value);
    }
}