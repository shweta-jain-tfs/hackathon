using System.ComponentModel;
using Microsoft.Extensions.Localization;
using Pharos.Client.Common.Localization;

namespace Pharos.Client.App.Settings.ViewModels;

public abstract class SettingsViewModelBase(IStringLocalizer<Translations> loc) : ViewModelBase
{
    private const string _propertyValue = "Value";
    protected IStringLocalizer<Translations> Loc { get; } = loc;

    public bool IsModified
    {
        get;
        set
        {
            if (value != field)
            {
                field = value;
                OnPropertyChanged(nameof(IsModified));
            }
        }
    }

    public bool IsValid
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged(nameof(IsValid));
            }
        }
    } = true;

    public List<string> ValidationErrors
    {
        get;
        set
        {
            // prevent unnecessary OnPropertyChanged raises
            if (field != value && ((field.Count != value.Count) ||
                                   (field.Count == value.Count && field.Count != 0)))
            {
                field = value;
                OnPropertyChanged(nameof(ValidationErrors));
            }
        }
    } = [];

    public SettingItemBase? Model { get; set; }

    public string Label => Model!.Label;
    public string LabelHelpText => Model!.LabelHelpText;

    public string Url => Model!.Url;

    public string EncodedUrl => string.Join('/', Model!.Url.Split('/').Select(Uri.EscapeDataString));

    public string? Key => Model?.Key;

    public string OriginPage { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether this view model represents a new, unsaved page list entry.
    /// </summary>
    public virtual bool IsLocal { get; set; }

    /// <summary>
    /// Used for the dependency, this is the value the dependency should have for it to be visible/enable
    /// </summary>
    public List<Dependency>? Dependency => Model?.Dependency.ToList();

    public List<SettingsViewModelBase>? DependencyViewModel
    {
        get;
        set
        {
            // Unregister from old items
            if (field != null)
            {
                foreach (var dep in field)
                {
                    dep.PropertyChanged -= DependencyOnPropertyChanged;
                }
            }

            field = value;
            if (field == null)
            {
                return;
            }

            foreach (var dep in field)
            {
                dep.PropertyChanged += DependencyOnPropertyChanged;
            }

            // Check initial values
            ValidateDependencies(field);
        }
    }

    private void DependencyOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == _propertyValue && sender is SettingsViewModelBase && DependencyViewModel != null && DependencyViewModel.Count != 0)
        {
            var shouldValidateSettings = ValidateDependencies(DependencyViewModel);

            if (shouldValidateSettings)
            {
                ValidateSetting();
            }
        }
    }

    /// <summary>
    /// Evaluates if the current setting should be enabled based on its dependencies.
    /// Clears validation errors if the setting is disabled or hidden due to unmet dependencies.
    /// </summary>
    /// <param name="dependencyViewModels">List of view models this setting depends on</param>
    /// <returns>True if all dependencies are met and the setting should be enabled; otherwise, false</returns>
    private bool ValidateDependencies(List<SettingsViewModelBase> dependencyViewModels)
    {
        IsDependencyEnabled = dependencyViewModels.All(d => (Dependency?.Where(dep => dep.MainOptionKey == d.Key)
                                    .Any(dep => d.MatchesDependencyValue(dep.MainOptionValue))) ?? true);
        if (!IsDependencyEnabled)
        {
            // Setting is hidden or disabled
            ValidationErrors = [];
            return false;
        }
        return true;
    }

    /// <summary>
    /// Gets or sets a value indicating whether this setting is enabled and visible
    /// based on the evaluation of its dependency conditions. If true, the setting is enabled/visible.
    /// </summary>
    public bool IsDependencyEnabled { get; set; } = true;

    public virtual bool Save()
    {
        return true;
    }

    public virtual bool ResetToDefault()
    {
        return true;
    }

    public virtual void ClearValidationErrors()
    {
        ValidationErrors = [];
        IsValid = true;
    }

    /// <summary>
    /// Determines whether this setting's current value matches the expected dependency value.
    /// Used to evaluate if dependency conditions are satisfied for enabling/showing dependent settings.
    /// </summary>
    /// /// <param name="expectedValue">The expected value that this setting should have for the dependency to be met</param>
    /// <returns>True if the current value matches the expected value; otherwise, false</returns>
    protected virtual bool MatchesDependencyValue(string? expectedValue)
    {
        return true;
    }

    protected virtual void ValidateSetting() { }

}
