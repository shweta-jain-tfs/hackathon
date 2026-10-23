using System.Linq.Expressions;
using Microsoft.AspNetCore.Components.Forms;
using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.Label;

/// <summary>
/// Label component used by most input elements (e.g. PhTextBox, PhNumberField, PhDropdown).
/// Normally included automatically; use directly when you need horizontal label layout
/// or when labeling non-standard components or custom content.
/// </summary>
public partial class PhLabel : IDisposable
{
    [CascadingParameter] public EditContext? CascadedEditContext { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Renders no label text but keeps the vertical spacing as if a label were shown.
    /// </summary>
    [Parameter] public bool IsBlankKeepDistance { get; set; }

    [Parameter] public LabelDirection Direction { get; set; } = LabelDirection.Vertical;
    [Parameter] public LabelColor Color { get; set; } = LabelColor.Default;
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public LabelSize Size { get; set; } = LabelSize.Body2;
    [Parameter] public LabelStyle Style { get; set; } = LabelStyle.Default;
    [Parameter] public string Text { get; set; } = "";
    [Parameter] public string? HelpText { get; set; } = string.Empty;
    [Parameter] public string? For { get; set; }

    /// <summary>
    /// Error text shown instead of help text. Set automatically if <see cref="ValidationFor"/> is set.
    /// </summary>
    [Parameter] public string? ErrorText { get; set; }
    [Parameter] public Expression<Func<object>>? ValidationFor { get; set; }
    [Parameter] public bool RenderAsLabel { get; set; } = true;
    [Parameter] public bool SuppressAutoValidation { get; set; }

    private readonly EventHandler<ValidationStateChangedEventArgs> _validationStateChangedHandler;
    private FieldIdentifier? _fieldIdentifier;
    private string? _errorText;
    private EditContext? _editContext;

    protected string CssClasses => new CssBuilder("ph-label")
        .AddClass("ph-label--direction-horizontal", Direction == LabelDirection.Horizontal)
        .AddClass("ph-label--disabled", Disabled)
        .AddClass("ph-label--color-discrete", Color == LabelColor.Discrete && !Disabled)
        .AddClass("ph-label--color-highlight", Color == LabelColor.Highlight && !Disabled)
        .AddClass("ph-label--color-warning", Color == LabelColor.Warning && !Disabled)
        .AddClass("t-helper-text", Size == LabelSize.HelperText)
        .AddClass("t-body2", Size == LabelSize.Body2)
        .AddClass("t-body1", Size == LabelSize.Body1)
        .AddClass("font-bold", Style == LabelStyle.Bold)
        .Build();

    private bool AllParametersAreEmpty => new List<string?> { Text, HelpText, ErrorText, For, _errorText }
        .All(string.IsNullOrEmpty);

    private bool HasFieldContent =>
        ChildContent is not null ||
        !string.IsNullOrEmpty(HelpText) ||
        !string.IsNullOrEmpty(_errorText);

    public PhLabel()
    {
        _validationStateChangedHandler = OnValidateStateChanged;
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (SuppressAutoValidation)
        {
            _editContext?.OnValidationStateChanged -= _validationStateChangedHandler;
        }
        else if (!_fieldIdentifier.HasValue && ValidationFor is not null)
        {
            _fieldIdentifier = FieldIdentifier.Create(ValidationFor);
            if (CascadedEditContext is not null)
            {
                _editContext = CascadedEditContext;
                // pick up validation messages from validation state changed to make sure
                // the text is updated for components that don't cause a re-render, like date picker.
                _editContext.OnValidationStateChanged += _validationStateChangedHandler;
            }
        }

        UpdateValidationError();
    }

    private void UpdateValidationError()
    {
        if (SuppressAutoValidation)
        {
            _errorText = ErrorText; // manual only
            return;
        }

        if (string.IsNullOrEmpty(ErrorText) && CascadedEditContext is not null && _fieldIdentifier.HasValue)
        {
            _errorText = CascadedEditContext.GetValidationMessages(_fieldIdentifier.Value).FirstOrDefault();
        }
        else
        {
            _errorText = ErrorText;
        }
    }

    private void OnValidateStateChanged(object? sender, ValidationStateChangedEventArgs e)
    {
        var currentError = _errorText;
        UpdateValidationError();
        if (currentError != _errorText)
        {
            StateHasChanged();
        }
    }

    public static Expression<Func<object>> CreateValidationForExpression<T>(Expression<Func<T>> valueExpression)
    {
        return Expression.Lambda<Func<object>>(
            Expression.Convert(valueExpression.Body, typeof(object)),
            valueExpression.Parameters
        );
    }

    public void Dispose()
    {
        if (_editContext is not null)
        {
            _editContext.OnValidationStateChanged -= _validationStateChangedHandler;
        }

        GC.SuppressFinalize(this);
    }
}