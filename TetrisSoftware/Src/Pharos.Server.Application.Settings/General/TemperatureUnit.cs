using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.General;

public enum TemperatureUnit
{
    [DisplayNameLocal("TemperatureUnit_Celcius")]
    Celsius = 0,

    [DisplayNameLocal("TemperatureUnit_Fahrenheit")]
    Fahrenheit = 1
}
