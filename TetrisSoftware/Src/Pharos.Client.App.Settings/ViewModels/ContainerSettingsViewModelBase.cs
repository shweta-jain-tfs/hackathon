using System.ComponentModel;
using Microsoft.Extensions.Localization;
using Pharos.Client.Common.Localization;

namespace Pharos.Client.App.Settings.ViewModels;

public abstract class ContainerSettingsViewModelBase(IStringLocalizer<Translations> loc) : SettingsViewModelBase(loc)
{
    private const string _valuePropertyName = "Value";
    private static readonly List<ISettingsViewModelCrossValidator> _crossValidators =
    [
        new NonOverlappingRangesSettingsViewModelValidator()
    ];

    public List<SettingsViewModelBase> Items
    {
        get;
        set
        {
            if (field != null)
            {
                foreach (var item in field)
                {
                    item.PropertyChanged -= ItemOnPropertyChanged;
                }
            }

            field = value;
            foreach (var item in field)
            {
                item.PropertyChanged += ItemOnPropertyChanged;
            }
        }
    } = [];

    public void Validate()
    {
        foreach (var item in Items)
        {
            ValidateItem(item);
        }

        ValidateCrossSettings();
        IsValid = AreItemsValid();
    }

    public void ApplyServerValidationErrors(IEnumerable<FailedSettingUpdateError> errors)
    {
        var options = GetOptionsByKey();

        foreach (var error in errors)
        {
            ApplyValidationError(options, error.Key, error.Error);
        }

        IsValid = AreItemsValid();
    }

    public override void ClearValidationErrors()
    {
        foreach (var item in Items)
        {
            item.ClearValidationErrors();
        }

        base.ClearValidationErrors();
    }

    private bool AreItemsValid()
    {
        return !Items.Any(c => c.ValidationErrors.Count != 0) && Items.All(c => c.IsValid);
    }

    private void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IsModified))
        {
            IsModified = Items.Any(c => c.IsModified);
        }

        if (e.PropertyName == _valuePropertyName)
        {
            ValidateCrossSettings();
            OnPropertyChanged(_valuePropertyName);
        }

        if (e.PropertyName is (nameof(ValidationErrors)) or (nameof(IsValid)))
        {
            IsValid = AreItemsValid();
        }
    }

    private void ValidateCrossSettings()
    {
        if (_crossValidators.Count == 0)
        {
            return;
        }

        var options = GetOptionsByKey();
        ClearCrossValidationErrors(options.Values);

        foreach (var error in _crossValidators.SelectMany(validator => validator.Validate(options)))
        {
            ApplyValidationError(options, error.Key, error.Error);
        }

        IsValid = AreItemsValid();
    }

    private void ClearCrossValidationErrors(IEnumerable<OptionViewModelBase> options)
    {
        var managedMessages = _crossValidators
            .SelectMany(validator => validator.ManagedErrors)
            .Distinct()
            .Select(GetValidationMessage)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var option in options)
        {
            var errors = option.ValidationErrors
                .Where(error => !managedMessages.Contains(error))
                .ToList();

            if (errors.Count != option.ValidationErrors.Count)
            {
                option.ValidationErrors = errors;
            }
        }
    }

    private void ApplyValidationError(
        Dictionary<string, OptionViewModelBase> options,
        string key,
        SettingUpdateError error)
    {
        if (!options.TryGetValue(key, out var option))
        {
            return;
        }

        var message = GetValidationMessage(error);
        if (!option.ValidationErrors.Contains(message))
        {
            option.ValidationErrors = [..option.ValidationErrors, message];
        }
    }

    private string GetValidationMessage(SettingUpdateError error)
    {
        var localized = Loc[$"SettingUpdateError_{error}"];
        return localized.ResourceNotFound ? Loc["Validation_ValidationError"].Value : localized.Value;
    }

    private Dictionary<string, OptionViewModelBase> GetOptionsByKey()
    {
        return FindOptions(Items)
            .Where(option => option.Key is not null)
            .GroupBy(option => option.Key!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
    }

    private static void ValidateItem(SettingsViewModelBase item)
    {
        if (!item.IsDependencyEnabled)
        {
            return;
        }

        switch (item)
        {
            case OptionViewModelBase option:
                option.Validate();
                break;
            case ContainerSettingsViewModelBase container:
                container.Validate();
                break;
        }
    }

    private static IEnumerable<OptionViewModelBase> FindOptions(IEnumerable<SettingsViewModelBase> items)
    {
        foreach (var item in items)
        {
            switch (item)
            {
                case OptionViewModelBase option:
                    yield return option;
                    break;
                case ContainerSettingsViewModelBase container:
                    foreach (var childOption in FindOptions(container.Items))
                    {
                        yield return childOption;
                    }

                    break;
            }
        }
    }
}
