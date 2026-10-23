using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.General;

public class RegionalSettings
{
    [DisplayNameLocal("GeneralSettings_RegionalSettings_Culture")]
    [HelpText(typeof(string), "GeneralSettings_RegionalSettings_Culture_NeedsLogin")]
    public UICulture Culture { get; set; } = UICulture.en_US;

    [DisplayNameLocal("GeneralSettings_RegionalSettings_TemperatureUnit")]
    public TemperatureUnit TemperatureUnit { get; set; } = TemperatureUnit.Celsius;

    [DisplayNameLocal("GeneralSettings_RegionalSettings_TimeZone")]
    [DynamicDropdown(typeof(TimeZoneDropdownProvider))]
    public string TimeZone { get; set; } = "UTC";
}