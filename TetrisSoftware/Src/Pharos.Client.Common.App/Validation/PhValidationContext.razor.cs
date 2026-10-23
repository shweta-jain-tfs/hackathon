using Microsoft.AspNetCore.Components.Forms;

namespace Pharos.Client.Common.App.Validation;

/// <summary>
/// Collects validation errors from multiple sections of a page in one place.
/// Also allows triggering validation of all sections at once, e.g. before saving.
/// </summary>
public partial class PhValidationContext : IDisposable
{
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public EventCallback<ValidationContextState> OnValidationStateChanged { get; set; }

    private readonly Dictionary<string, ValidationSectionInfo> _sectionValidationStates = [];
    private EventHandler<ValidationStateChangedEventArgs>? _validationStateChangedHandler;
    private bool _notifyValidationStateChangedDueToReplacedModel;

    protected override void OnInitialized()
    {
        _validationStateChangedHandler = SectionStateChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_notifyValidationStateChangedDueToReplacedModel)
        {
            _notifyValidationStateChangedDueToReplacedModel = false;
            if (OnValidationStateChanged.HasDelegate)
            {
                await OnValidationStateChanged.InvokeAsync(GetValidationState());
            }
        }
    }

    /// <summary>
    /// Revalidates all sections. Call this when discarding changes or saving.
    /// Not needed if the model for a section is replaced with a new object. That will trigger a forced validation refresh.
    /// </summary>
    public void Reset()
    {
        foreach (var section in _sectionValidationStates.Values)
        {
            section.EditContext?.MarkAsUnmodified();
            section.EditContext?.Validate();
        }
        // updated models will give a delayed update via AddSection
    }

    /// <summary>
    /// Notifies the EditContext that a field has changed, triggering re-validation for that field.
    /// This is needed when a property is updated programmatically, for example, when setting the 'TestName'
    /// on the Condition page after a test is selected from a modal.
    /// </summary>
    public void NotifyFieldChanged<TField>(System.Linq.Expressions.Expression<Func<TField>> fieldAccessor)
    {
        var fieldIdentifier = FieldIdentifier.Create(fieldAccessor);

        foreach (var section in _sectionValidationStates.Values)
        {
            section.EditContext?.NotifyFieldChanged(fieldIdentifier);
        }
    }

    public bool IsValid()
    {
        foreach (var section in _sectionValidationStates)
        {
            if (!IsValid(section.Key))
            {
                return false;
            }
        }

        return true;
    }

    public bool IsValid(string sectionName)
    {
        if (_sectionValidationStates.TryGetValue(sectionName, out var section))
        {
            return !section.EditContext?.GetValidationMessages().Any() ?? false;
        }

        // section is unknown until the page has finished rendering once
        return true;
    }

    public bool Validate()
    {
        var isAllValid = true;
        foreach (var section in _sectionValidationStates)
        {
            isAllValid &= Validate(section.Key);
        }

        return isAllValid;
    }

    public bool Validate(string sectionName)
    {
        if (_sectionValidationStates.TryGetValue(sectionName, out var section))
        {
            return section.EditContext?.Validate() ?? false;
        }

        throw new InvalidOperationException($"Section '{sectionName}' not found in validation context.");
    }

    internal void AddSection(string sectionName, EditContext editContext)
    {
        var needsRefresh = false;

        if (_sectionValidationStates.TryGetValue(sectionName, out var oldSection))
        {
            if (oldSection.EditContext is not null)
            {
                oldSection.EditContext.OnValidationStateChanged -= _validationStateChangedHandler;
            }

            needsRefresh = oldSection.EditContext != editContext;
        }

        var section = new ValidationSectionInfo(sectionName, editContext);
        _sectionValidationStates[sectionName] = section;

        // add event listeners
        section.EditContext.OnValidationStateChanged += _validationStateChangedHandler;

        if (needsRefresh)
        {
            _notifyValidationStateChangedDueToReplacedModel = true;
            StateHasChanged();
        }
    }

    private void SectionStateChanged(object? sender, ValidationStateChangedEventArgs e)
    {
        if (OnValidationStateChanged.HasDelegate)
        {
            OnValidationStateChanged.InvokeAsync(GetValidationState());
        }
    }

    private ValidationContextState GetValidationState()
    {
        var sectionStates = _sectionValidationStates.ToDictionary(x => x.Key, x => IsValid(x.Key));
        return new ValidationContextState(sectionStates);
    }

    public void Dispose()
    {
        foreach (var section in _sectionValidationStates.Values)
        {
            if (section.EditContext is not null)
            {
                section.EditContext.OnValidationStateChanged -= _validationStateChangedHandler;
            }
        }

        _sectionValidationStates.Clear();
        GC.SuppressFinalize(this);
    }
}

internal record ValidationSectionInfo(string Name, EditContext EditContext);