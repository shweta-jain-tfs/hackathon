using Tetris.Protocol.Common;

namespace Pharos.Server.Instrument.Application.Queries.GetInstrumentEnvironment;

public class SensorValueDto
{
    public double? Value { get; set; }
    public double? LowerLimit { get; set; }
    public double? UpperLimit { get; set; }
    public EnumContinuousSensorType Sensor { get; set; }
    public bool OutsideLimit { get; set; }
}