namespace Pharos.Client.App.Settings.ViewModels.Converters;

/// <summary>
/// Defines a converter for specific component types within the settings system.
/// Implementations of this interface are responsible for converting setting items to their
/// corresponding view model representations.
/// </summary>
public interface IComponentConverter
{
    /// <summary>
    /// Gets the type of the component that this converter can handle.
    /// </summary>
    Type Type { get; }

    /// <summary>
    /// Tries to convert a settings item to a view model.
    /// </summary>
    /// <param name="item">The settings item to convert.</param>
    /// <param name="converter">The converter instance.</param>
    /// <param name="dataService">The data service used to interact with the settings data.</param>
    /// <param name="result">The resulting view model if the conversion is successful.</param>
    /// <returns>True if the conversion is successful; otherwise false.</returns>
    bool TryConvert(SettingItemBase item, IConverter converter, ISettingsDataService<SettingItemBase> dataService, out SettingsViewModelBase? result);

}