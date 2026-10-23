using System.Linq.Expressions;
using Microsoft.AspNetCore.Components.Forms;
using Pharos.Common.Web.Extensions;
using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.FormElements.RadioButton;

/// <summary>
/// A group of radio buttons for selecting a single value, with optional validation support via <see cref="EditContext"/>.
/// </summary>
public partial class PhRadioGroup<T>
{
    [CascadingParameter] public EditContext? CascadedEditContext { get; set; }

    /// <summary>
    /// The currently selected value in the group.
    /// </summary>
    [Parameter] public T? SelectedValue { get; set; }

    /// <summary>
    /// Raised when the selected value changes.
    /// </summary>
    [Parameter] public EventCallback<T?> SelectedValueChanged { get; set; }

    /// <summary>
    /// Represents the bound selected value and is set automatically when using @bind.
    /// </summary>
    [Parameter] public Expression<Func<T>>? SelectedValueExpression { get; set; }

    /// <summary>
    /// The radio buttons to render inside this group.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Displays the radio buttons horizontally instead of vertically.
    /// </summary>
    [Parameter] public bool IsHorizontal { get; set; } = false;

    /// <summary>
    /// Renders the component as display-only (non-interactive, no automatic validation).
    /// Overrides <see cref="PhRadioButton{T}.Disabled"/>, but keeps the disabled label and value color.
    /// </summary>
    [Parameter] public bool DisplayOnly { get; set; }

    /// <summary>
    /// Manually marks the group as having validation errors, overriding the <see cref="EditContext"/> validation state.
    /// </summary>
    [Parameter] public bool Error { get; set; }

    // Generate a unique name for this group.
    public string GroupName { get; } = Guid.NewGuid().ToString();

    private FieldIdentifier? _fieldIdentifier;

    protected string Classname => new CssBuilder("ph-radio-group")
        .AddClass("ph-radio-group--horizontal", IsHorizontal)
        .Build();

    protected string? AutomatedTestId { get; set; }
    internal bool HasError { get; private set; }

    public async Task UpdateSelectedValue(T? value)
    {
        if (DisplayOnly)
        {
            return;
        }

        SelectedValue = value;
        await SelectedValueChanged.InvokeAsync(value);
        NotifyFieldChanged();
    }

    private void NotifyFieldChanged()
    {
        if (DisplayOnly)
        {
            return;
        }

        if (_fieldIdentifier.HasValue)
        {
            CascadedEditContext?.NotifyFieldChanged(_fieldIdentifier.Value);
        }
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!_fieldIdentifier.HasValue && SelectedValueExpression is not null)
        {
            _fieldIdentifier = FieldIdentifier.Create(SelectedValueExpression);
            AutomatedTestId = this.CreateAutomatedTestId(_fieldIdentifier.Value);
        }

        if (DisplayOnly)
        {
            HasError = Error;
            return;
        }

        if (!Error && CascadedEditContext is not null && _fieldIdentifier.HasValue)
        {
            HasError = !CascadedEditContext.IsValid(_fieldIdentifier.Value);
        }
        else
        {
            HasError = Error;
        }
    }
}