using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Pharos.Stories.Components.TestContent;

public class MockValidator : ComponentBase
{
    private ValidationMessageStore? _messageStore;

    [CascadingParameter] public EditContext? CurrentEditContext { get; set; }

    [Parameter] public MockValidatorType Type { get; set; } = MockValidatorType.EverythingIsWrong;

    protected override void OnInitialized()
    {
        if (CurrentEditContext == null)
        {
            throw new InvalidOperationException($"{nameof(MockValidator)} requires a cascading " +
                $"parameter of type {nameof(EditContext)}. For example, you can use {nameof(MockValidator)} " +
                $"inside an {nameof(EditForm)}.");
        }

        _messageStore = new ValidationMessageStore(CurrentEditContext);

        CurrentEditContext.OnValidationRequested += OnValidationRequested;
        CurrentEditContext.OnFieldChanged += OnFieldChanged;
    }

    public void ClearErrors()
    {
        _messageStore?.Clear();
        CurrentEditContext?.NotifyValidationStateChanged();
    }

    private void OnValidationRequested(object? sender, ValidationRequestedEventArgs e)
    {
        _messageStore?.Clear();
        if (CurrentEditContext?.Model is null)
        {
            return;
        }

        var propertyNames = CurrentEditContext.Model.GetType().GetProperties().Select(x => x.Name);

        foreach (var prop in propertyNames)
        {
            var identifier = CurrentEditContext.Field(prop);
            if (!IsValid(identifier))
            {
                _messageStore?.Add(identifier, $"{prop} is invalid {DateTime.Now:T} {nameof(OnValidationRequested)} (value=[{CurrentValue(identifier)}]).");
            }
        }

        CurrentEditContext?.NotifyValidationStateChanged();
    }

    private void OnFieldChanged(object? sender, FieldChangedEventArgs e)
    {
        _messageStore?.Clear(e.FieldIdentifier);
        if (!IsValid(e.FieldIdentifier))
        {
            _messageStore?.Add(e.FieldIdentifier, $"{e.FieldIdentifier.FieldName} is invalid {DateTime.Now:T} {nameof(OnFieldChanged)} (value=[{CurrentValue(e.FieldIdentifier)}]).");
        }

        CurrentEditContext?.NotifyValidationStateChanged();
    }

    private object? CurrentValue(FieldIdentifier identifier)
    {
        return CurrentEditContext?.Model.GetType().GetProperty(identifier.FieldName)?.GetValue(CurrentEditContext.Model, null);
    }

    private bool IsValid(FieldIdentifier identifier)
    {
        if (Type == MockValidatorType.MinLength)
        {
            var value = CurrentValue(identifier)?.ToString();
            return value is { Length: >= 8 };
        }

        if (Type == MockValidatorType.Range)
        {
            double? value = CurrentValue(identifier) switch
            {
                int number => number,
                double number => number,
                _ => null
            };

            return value is >= 0 and <= 100;
        }

        if (Type == MockValidatorType.Randomize)
        {
            return new Random().Next(0, 2) == 0;
        }

        return Type == MockValidatorType.EverythingIsRight;
    }
}

public enum MockValidatorType
{
    EverythingIsWrong,
    EverythingIsRight,
    Randomize,
    MinLength,
    Range,
}
