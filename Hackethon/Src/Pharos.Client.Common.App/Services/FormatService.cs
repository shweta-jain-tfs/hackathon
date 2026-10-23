namespace Pharos.Client.Common.App.Services;

/// <summary>
/// Service that provides formatting functionality based on user preferences.
/// </summary>
public class FormatService(UiSettingsService uiSettingsService)
{
    #region Temperature Formatting
    /// <summary>
    /// Converts temperature from Celsius to preferred unit.
    /// </summary>
    public static double ConvertCelsiusToTemperatureUnit(double celsius, TemperatureUnit temperatureUnit)
    {
        return temperatureUnit switch
        {
            TemperatureUnit.Celsius => celsius,
            TemperatureUnit.Fahrenheit => (celsius * 9.0 / 5.0) + 32.0,
            _ => throw new ArgumentOutOfRangeException(nameof(celsius), temperatureUnit, "Unsupported temperature unit.")
        };
    }

    /// <summary>
    /// Gets the current temperature unit from settings.
    /// </summary>
    public async Task<TemperatureUnit> GetTemperatureUnitAsync()
    {
        var settings = await uiSettingsService.GetSettingsAsync();
        return settings.RegionalSettings.TemperatureUnit;
    }
    #endregion
}