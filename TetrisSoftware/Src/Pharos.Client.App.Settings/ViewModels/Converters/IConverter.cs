namespace Pharos.Client.App.Settings.ViewModels.Converters;

/// <summary>
/// Defines a converter that transforms model objects (<see cref="SettingItemBase"/>)
/// into view model objects (<see cref = "SettingsViewModelBase" />).
/// </summary>
public interface IConverter
{
    /// <summary>
    /// Converts a settings item to a view model.
    /// </summary>
    /// <param name="item">The settings item to convert.</param>
    /// <returns>The converted view model.</returns>
    SettingsViewModelBase Convert(SettingItemBase item);
}