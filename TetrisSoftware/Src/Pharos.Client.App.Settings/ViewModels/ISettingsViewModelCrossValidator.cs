namespace Pharos.Client.App.Settings.ViewModels;

internal interface ISettingsViewModelCrossValidator
{
    IReadOnlyCollection<SettingUpdateError> ManagedErrors { get; }

    IEnumerable<SettingsViewModelCrossValidationError> Validate(IReadOnlyDictionary<string, OptionViewModelBase> options);
}

internal sealed record SettingsViewModelCrossValidationError(string Key, SettingUpdateError Error);
