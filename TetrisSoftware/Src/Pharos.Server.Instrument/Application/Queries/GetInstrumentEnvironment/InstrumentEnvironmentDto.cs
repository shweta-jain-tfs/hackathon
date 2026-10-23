namespace Pharos.Server.Instrument.Application.Queries.GetInstrumentEnvironment;
public class InstrumentEnvironmentDto
{
    public List<SensorValueDto> HumidityList { get; set; } = new();
    public List<SensorValueDto> TemperatureList { get; set; } = new();
}
