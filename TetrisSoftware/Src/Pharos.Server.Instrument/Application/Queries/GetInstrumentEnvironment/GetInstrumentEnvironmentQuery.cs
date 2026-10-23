using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Exceptions;
using Tetris.Protocol.Pharos;

namespace Pharos.Server.Instrument.Application.Queries.GetInstrumentEnvironment;

internal record GetInstrumentEnvironmentQuery(int InstrumentId) : IRequest<InstrumentEnvironmentDto>;

internal class GetInstrumentEnvironmentQueryHandler(IInstrumentManager instrumentManager) : IRequestHandler<GetInstrumentEnvironmentQuery, InstrumentEnvironmentDto>
{
    public Task<InstrumentEnvironmentDto> Handle(GetInstrumentEnvironmentQuery request, CancellationToken cancellationToken)
    {
        var instrument = (Domain.Instrument)instrumentManager.GetInstrument(request.InstrumentId);

        if (instrument == null)
        {
            throw new NotFoundException();
        }

        var environmentDto = new InstrumentEnvironmentDto
        {
            HumidityList = GetSensors(instrument.EnvironmentSensors
                .Where(x => x.Unit == EnumUnit.RelativeHumidity)
                .OrderByDescending(x => x.OutsideLimit)
                .ThenBy(x => x.Type)
            ),

            TemperatureList = GetSensors(instrument.EnvironmentSensors
                .Where(x => x.Unit == EnumUnit.Celsius)
                .OrderByDescending(x => x.OutsideLimit)
                .ThenBy(x => x.Type)
            )
        };

        return Task.FromResult(environmentDto);
    }

    private static List<SensorValueDto> GetSensors(IEnumerable<ContinuousSensor> sensors)
    {
        return sensors.Select(sensor => new SensorValueDto
        {
            Value = sensor.Value,
            LowerLimit = sensor.LowerLimit,
            UpperLimit = sensor.UpperLimit,
            Sensor = sensor.Type,
            OutsideLimit = sensor.OutsideLimit
        }).ToList();
    }
}